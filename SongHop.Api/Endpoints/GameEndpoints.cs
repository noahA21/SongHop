// SongHop.Api/Endpoints/GameEndpoints.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SongHop.Core.Interfaces;
using SongHop.Core.Models;
using SongHop.Data;

namespace SongHop.Api.Endpoints;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        // Establish the global group matching your Angular environment configurations
        var group = app.MapGroup("/v1")
                       .RequireCors("AllowAngularDev")
                       .WithTags("Game Engine");

        // 1. 🔍 ARTIST SEARCH (Preserved Exactly as Working)
        group.MapGet("/node/search", async ([FromQuery] string q, SongHopDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(q)) 
            {
                return Results.Ok(new List<Node>());
            }

            var results = await db.Nodes
                .AsNoTracking()
                .Where(n => EF.Functions.ILike(n.Name, $"%{q}%"))
                .Take(15)
                .ToListAsync();

            return Results.Ok(results);
        });

        // 2. 🎮 WINNABLE SESSION GENERATOR (Guarantees 2 to 5 Hop Paths)
        group.MapGet("/game/start", async (SongHopDbContext db, IPathfindingService pathfinder) =>
        {
            var nodes = await db.Nodes.AsNoTracking().ToListAsync();
            if (nodes.Count < 2) return Results.BadRequest("Insufficient node data in database.");

            var random = new Random();
            int maxAttempts = 100;
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                var startNode = nodes[random.Next(nodes.Count)];
                var targetNode = nodes[random.Next(nodes.Count)];

                if (startNode.Id == targetNode.Id) continue;

                // Pre-calculate shortest path to guarantee winnability
                var pathResult = await pathfinder.FindShortestPathAsync(startNode.Id, targetNode.Id);

                if (pathResult.IsValid && pathResult.MoveCount >= 2 && pathResult.MoveCount <= 5)
                {
                    return Results.Ok(new
                    {
                        StartNode = startNode,
                        TargetNode = targetNode,
                        OptimalHops = pathResult.MoveCount
                    });
                }
                attempt++;
            }

            // Fallback Edge Case: Find any connected graph elements if 2-5 hop constraint fails
            foreach (var startNode in nodes.OrderBy(_ => random.Next()).Take(10))
            {
                foreach (var targetNode in nodes.Where(n => n.Id != startNode.Id).OrderBy(_ => random.Next()).Take(10))
                {
                    var pathResult = await pathfinder.FindShortestPathAsync(startNode.Id, targetNode.Id);
                    if (pathResult.IsValid)
                    {
                        return Results.Ok(new { StartNode = startNode, TargetNode = targetNode, OptimalHops = pathResult.MoveCount });
                    }
                }
            }

            return Results.BadRequest("Could not isolate a connected pair of artists. Ingest more edges via SongHopCrawler.");
        });

        // 3. 🧠 BIDIRECTIONAL NEIGHBOR EXPANSION (Prevents dead ends)
        group.MapGet("/node/expand/{id:guid}", async (Guid id, SongHopDbContext db) => 
        {
            var edges = await db.Edges
                .AsNoTracking()
                .Where(e => e.SourceId == id || e.TargetId == id)
                .ToListAsync();

            var neighborIds = edges
                .Select(e => e.SourceId == id ? e.TargetId : e.SourceId)
                .Distinct()
                .Where(nodeId => nodeId != id) 
                .ToList();

            var connectedNodes = await db.Nodes
                .AsNoTracking()
                .Where(n => neighborIds.Contains(n.Id))
                .ToListAsync();
            
            return Results.Ok(new { Edges = edges, Nodes = connectedNodes });
        });

        // 4. 🧭 SMART PATHFINDING ENDPOINT (For background calculation hints)
        group.MapPost("/path/smart", async ([FromBody] FindPathRequest request, IPathfindingService pathfinder) => 
        {
            var result = await pathfinder.FindShortestPathAsync(request.StartNodeId, request.TargetNodeId);
            if (result.IsValid) return Results.Ok(result);
            return Results.NotFound(new { Message = "No path found." });
        });

        // 5. 🛡️ AUTHENTIC PATH VALIDATION ENDPOINT (Resolves Endless Hops)
        group.MapPost("/path/validate", async ([FromBody] ValidatePathRequest request, SongHopDbContext db) => 
        {
            // Edge Case: Empty or fragmented payload lists
            if (request.SubmittedPath == null || request.SubmittedPath.Count < 2)
            {
                return Results.Ok(new { IsValid = false, Message = "A valid path requires a minimum of 2 connected nodes." });
            }

            // Structural Loop: Verify every connection against actual database edges
            for (int i = 0; i < request.SubmittedPath.Count - 1; i++)
            {
                if (!Guid.TryParse(request.SubmittedPath[i], out Guid currentId) || 
                    !Guid.TryParse(request.SubmittedPath[i + 1], out Guid nextId))
                {
                    return Results.Ok(new { IsValid = false, Message = "Malformed graph node identifiers encountered." });
                }

                bool linkExists = await db.Edges.AsNoTracking().AnyAsync(e => 
                    (e.SourceId == currentId && e.TargetId == nextId) || 
                    (e.SourceId == nextId && e.TargetId == currentId));

                if (!linkExists)
                {
                    return Results.Ok(new { IsValid = false, Message = "Verification failed. Broken link sequence detected." });
                }
            }

            return Results.Ok(new { IsValid = true, MoveCount = request.SubmittedPath.Count - 1 });
        });

        // 6. 🧪 DATA BASING CHECK ENDPOINT
        group.MapGet("/test/nodes", async (SongHopDbContext db) => 
        {
            return Results.Ok(await db.Nodes.AsNoTracking().ToListAsync());
        });
    }
}

// Data Transfer Records resolving Minimal API model reflection
public record FindPathRequest(Guid StartNodeId, Guid TargetNodeId);
public record ValidatePathRequest(List<string> SubmittedPath);