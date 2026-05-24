using Microsoft.AspNetCore.Mvc;
using SongHop.Api.Mocks;
using SongHop.Core.Interfaces;
using SongHop.Pathing.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection setup
// Register our mock database as a Singleton (so it keeps data in memory)
builder.Services.AddSingleton<MockGraphRepository>();
// Map the interface to the mock implementation
builder.Services.AddSingleton<IGraphRepository>(sp => sp.GetRequiredService<MockGraphRepository>());
// Register the engine we wrote earlier
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

// Helper endpoint just to get our fake IDs for testing
app.MapGet("/v1/test/nodes", (MockGraphRepository repo) => 
{
    return Results.Ok(repo.GetAllNodes());
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