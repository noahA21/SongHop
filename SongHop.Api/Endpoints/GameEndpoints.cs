// SongHop.Api/Endpoints/GameEndpoints.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SongHop.Data;
using SongHop.Core.Interfaces;
using SongHop.Core.Models;

namespace SongHop.Api.Endpoints;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1");

        // 🔍 FIXES THE SEARCH 404:
        group.MapGet("/node/search", async ([FromQuery] string q, SongHopDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(q)) return Results.Ok(new List<Node>());
            
            var results = await db.Nodes
                .AsNoTracking()
                .Where(n => EF.Functions.ILike(n.Name, $"%{q}%"))
                .Take(15)
                .ToListAsync();

            return Results.Ok(results);
        });

    group.MapGet("/game/start", async (SongHopDbContext db) =>
    {
        // Find all node IDs that actually appear inside your edges table
        var connectedNodeIds = await db.Edges
            .Select(e => e.SourceId)
            .Union(db.Edges.Select(e => e.TargetId))
            .Distinct()
            .ToListAsync();

        if (connectedNodeIds.Count < 2) return Results.BadRequest("Not enough graph data.");

        var random = new Random();
        
        // Pick a start node that definitely has branching links
        var startGuid = connectedNodeIds[random.Next(connectedNodeIds.Count)];
        var startNode = await db.Nodes.FindAsync(startGuid);

        // Pick a separate random target node from the entire database pool
        var totalNodes = await db.Nodes.AsNoTracking().ToListAsync();
        var targetNode = totalNodes.Where(n => n.Id != startGuid).OrderBy(_ => random.Next()).First();

        return Results.Ok(new { StartNode = startNode, TargetNode = targetNode });
    });

    group.MapGet("/node/expand/{id:guid}", async (Guid id, SongHopDbContext db) => 
    {
        // 🔍 FIX: Look for links where the current artist is EITHER the source OR the target
        var edges = await db.Edges
            .AsNoTracking()
            .Where(e => e.SourceId == id || e.TargetId == id)
            .ToListAsync();

        // Collect the opposing IDs from those edges
        var neighborIds = edges
         .Select(e => e.SourceId == id ? e.TargetId : e.SourceId)
            .Distinct()
            .Where(nodeId => nodeId != id) 
            .ToList();

     // Resolve those IDs into full Node objects to populate the UI buttons
        var connectedNodes = await db.Nodes
            .AsNoTracking()
            .Where(n => neighborIds.Contains(n.Id))
            .ToListAsync();
    
        return Results.Ok(new { Edges = edges, Nodes = connectedNodes });
    });
    }
}