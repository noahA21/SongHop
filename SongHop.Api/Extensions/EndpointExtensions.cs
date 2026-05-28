// Extensions/EndpointExtensions.cs
using SongHop.Core.Interfaces;
using SongHop.Core.Models;
using SongHop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace SongHop.Api.Extensions;

public static class EndpointExtensions
{
    public static void MapNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/node");

        // The endpoint the frontend uses to expand connections
        group.MapGet("/expand/{id:guid}", async (Guid id, IGraphRepository repo) => 
        {
            var edges = await repo.GetNeighborsAsync(id);
            var targetNodeIds = edges.Select(e => e.TargetId).Distinct();
            var connectedNodes = await repo.GetNodesByIdsAsync(targetNodeIds);
            
            return Results.Ok(new { Edges = edges, Nodes = connectedNodes });
        });

        // Add other node-specific endpoints here...
    }

    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/game");

        // The endpoint the frontend uses to check pathfinding
        group.MapPost("/path", async (
            [FromQuery] Guid startId, 
            [FromQuery] Guid targetId, 
            [FromServices] IPathfindingService pathfinder) => 
        {
            var result = await pathfinder.FindShortestPathAsync(startId, targetId);
            if (!result.IsValid) return Results.NotFound("No path found between these artists.");
            return Results.Ok(new { Steps = result.MoveCount, Path = result.NodeIds });
        });
    }
}