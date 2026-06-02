// Program.cs
using Microsoft.EntityFrameworkCore;
using SongHop.Data;
using SongHop.Data.Repositories;
using SongHop.Core.Interfaces;
using SongHop.Pathing.Services;
using SongHop.Api.Mocks;
using SongHop.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("'DefaultConnection' connection string is missing from configuration files.");

builder.Services.AddDbContext<SongHopDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
            // Read the hosting mode safely
            if (builder.Environment.IsDevelopment())
            {
                policy.WithOrigins("http://localhost:4200") // Local Angular development server
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                // 🛑 FIXED: Removed the trailing slash from the end of the URL string
                policy.WithOrigins("https://delightful-dune-0bdd6f010.7.azurestaticapps.net") 
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
});

// Configure Layer Repositories
builder.Services.AddSingleton<MockGraphRepository>();
builder.Services.AddSingleton<IGraphRepository>(provider => 
    provider.GetRequiredService<MockGraphRepository>());
builder.Services.AddScoped<IGraphRepository, PostgresGraphRepository>();

// Configure Pathfinding
builder.Services.AddScoped<PathfindingService>();
builder.Services.AddScoped<IPathfindingService, PathfindingService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularDev");

// 🎯 FIXED: Explicit health check route for the root directory to stop the 404 error
app.MapGet("/", () => "🎵 SongHop API is live and rocking!");

app.MapGameEndpoints();
app.MapNodeEndpoints();

app.Run();