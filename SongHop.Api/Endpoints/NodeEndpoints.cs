using SongHop.Core.Interfaces;
using SongHop.Data;
using Microsoft.EntityFrameworkCore;
using SongHop.Core.Models;

namespace SongHop.Api.Endpoints;

public static class NodeEndpoints
{
    public static void MapNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/node");

        // // The endpoint the frontend uses when a player clicks an artist to see their connections
        // group.MapGet("/expand/{id:guid}", async (Guid id, IGraphRepository repo) => 
        // {
        //     var edges = await repo.GetNeighborsAsync(id);
        //     var targetNodeIds = edges.Select(e => e.TargetId).Distinct();
        //     var connectedNodes = await repo.GetNodesByIdsAsync(targetNodeIds);
            
        //     return Results.Ok(new { Edges = edges, Nodes = connectedNodes });
        // });

        

        // 🌟 NEW: Full-text Database Query Search Engine
        // group.MapGet("/search", async (string q, SongHopDbContext db) =>
        // {
        //     if (string.IsNullOrWhiteSpace(q))
        //     {
        //         return Results.Ok(Array.Empty<Node>());
        //     }

        //     // Case-insensitive wildcard match using Postgres ILIKE, sorted by popularity
        //     var matchingArtists = await db.Nodes
        //         .Where(n => EF.Functions.ILike(n.Name, $"%{q}%"))
        //         .OrderByDescending(n => n.PopularityScore)
        //         .Take(10) // Limit output for high network performance
        //         .ToListAsync();

        //     return Results.Ok(matchingArtists);
        // });
    }
    
}