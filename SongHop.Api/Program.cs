using Microsoft.AspNetCore.Mvc;
using SongHop.Api.Mocks;
using SongHop.Core.Interfaces;
using SongHop.Pathing.Services;
using Microsoft.EntityFrameworkCore;
using SongHop.Data;
using SongHop.Data.Repositories;
using SongHop.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Hook up PostgreSQL Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=songhop_db;Username=postgres;Password=postgres_password";

builder.Services.AddDbContext<SongHopDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Angular's default port
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
// builder.Services.AddScoped<IGraphRepository, GraphRepository>();
// 1. Register the concrete mock so your test endpoint can use it
builder.Services.AddSingleton<MockGraphRepository>();

builder.Services.AddSingleton<IGraphRepository>(provider => 
    provider.GetRequiredService<MockGraphRepository>());

// 2. Register the real Postgres Repository
builder.Services.AddScoped<IGraphRepository, PostgresGraphRepository>();

// 3. Keep our pathfinding service (it seamlessly transitions from Mock to Postgres!)
builder.Services.AddScoped<IPathfindingService, PathfindingService>();

// Add Swagger for easy testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2. Define API Endpoints
app.UseCors("AllowAngularDev");
// Helper endpoint just to get our fake IDs for testing
app.MapGet("/v1/test/nodes", (MockGraphRepository repo) => 
{
    return Results.Ok(repo.GetAllNodes());
});

app.MapPost("/v1/admin/seed", async (SongHopDbContext db) =>
{
    if (await db.Nodes.AnyAsync())
    {
        return Results.Conflict("Database already has data.");
    }

    // You need to pass arguments in the exact order: (Guid, string, NodeType, int, string?, string?)
    // Replace 'NodeType.Artist' with your actual Enum value if you have one
    var artist1 = new Node(Guid.NewGuid(), "Daft Punk", NodeType.Artist, 100, null, null);
    var artist2 = new Node(Guid.NewGuid(), "Pharrell Williams", NodeType.Artist, 95, null, null);
    var artist3 = new Node(Guid.NewGuid(), "Nile Rodgers", NodeType.Artist, 90, null, null);

    await db.Nodes.AddRangeAsync(artist1, artist2, artist3);

    // Apply the same logic to Edge. 
    // Check your Edge class to see what parameters it requires in its constructor.
    var edges = new List<Edge>
    {
        // Example: If your Edge constructor is (Guid, Guid, Guid)
        new Edge(Guid.NewGuid(), artist1.Id, artist2.Id, EdgeType.RelatedArtist, true, 42),
        new Edge(Guid.NewGuid(), artist1.Id, artist3.Id, EdgeType.RelatedArtist, true, 65),
        new Edge(Guid.NewGuid(), artist2.Id, artist3.Id, EdgeType.RelatedArtist, true, 32)
    };

    await db.Edges.AddRangeAsync(edges);
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Seed successful" });
});

// The endpoint the frontend uses when a player clicks an artist to see their connections
app.MapGet("/v1/node/expand/{id:guid}", async (Guid id, IGraphRepository repo) => 
{
    var edges = await repo.GetNeighborsAsync(id);
    var targetNodeIds = edges.Select(e => e.TargetId).Distinct();
    var connectedNodes = await repo.GetNodesByIdsAsync(targetNodeIds);
    
    return Results.Ok(new { Edges = edges, Nodes = connectedNodes });
});

// The endpoint the background worker uses to find the daily challenge path
app.MapPost("/v1/path/smart", async ([FromBody] FindPathRequest request, IPathfindingService pathfinder) => 
{
    var result = await pathfinder.FindShortestPathAsync(request.StartNodeId, request.TargetNodeId);
    
    if (result.IsValid)
        return Results.Ok(result);
        
    return Results.NotFound(new { Message = "No path found within search depth limit." });
});

// The endpoint the frontend uses to validate a player's finished game
app.MapPost("/v1/path/validate", async ([FromBody] ValidatePathRequest request, IPathfindingService pathfinder) => 
{
    var result = await pathfinder.ValidatePathAsync(request.SubmittedPath);
    return Results.Ok(result);
});

app.Run();

// 3. Define the Request DTOs (Data Transfer Objects)
public record FindPathRequest(Guid StartNodeId, Guid TargetNodeId);
public record ValidatePathRequest(List<Guid> SubmittedPath);