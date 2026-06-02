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
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
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


app.MapGameEndpoints();
app.MapNodeEndpoints();

app.Run();