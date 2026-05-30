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
        var group = app.MapGroup("/v1")
                       .RequireCors("AllowAngularDev")
                       .WithTags("Game Engine");

        // 1. 🔍 ARTIST SEARCH
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

        // 2. 🎮 WINNABLE SESSION GENERATOR
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

        // 3. 🧠 BIDIRECTIONAL NEIGHBOR EXPANSION WITH DYNAMIC LINER NOTES
        app.MapGet("/v1/node/expand/{id}", async (
            Guid id, 
            [FromQuery] Guid? targetId,
            [FromServices] SongHopDbContext db,
            [FromServices] IPathfindingService pathfindingService) =>
        {
            // Fetch raw nodes connected via edges
            var connectedIds = await db.Edges
                .AsNoTracking()
                .Where(e => e.SourceId == id || e.TargetId == id)
                .Select(e => e.SourceId == id ? e.TargetId : e.SourceId)
                .Distinct()
                .ToListAsync();

            var rawNodes = await db.Nodes
                .AsNoTracking()
                .Where(n => connectedIds.Contains(n.Id))
                .ToListAsync();

            // Map raw database models into mutable Data Transfer Objects
            var rawNeighbors = rawNodes.Select(n => new NodeDto 
            { 
                Id = n.Id, 
                Name = n.Name, 
                PopularityScore = n.PopularityScore,
                Type = n.Type.ToString(),
                ConnectionReason = "Collaborative network track node connection."
            }).ToList();

            if (!rawNeighbors.Any())
            {
                return Results.Ok(new ExpandNodeResponse { Nodes = new List<NodeDto>(), CurrentDistance = null });
            }

            int? currentDistanceFromTarget = null;
            if (targetId.HasValue)
            {
                var currentPathEvaluation = await pathfindingService.FindShortestPathAsync(id, targetId.Value);
                if (currentPathEvaluation.IsValid)
                {
                    currentDistanceFromTarget = currentPathEvaluation.MoveCount;
                }
            }

            if (rawNeighbors.Count <= 5)
            {
                var immediatePool = rawNeighbors.OrderBy(_ => Random.Shared.Next()).ToList();
                return Results.Ok(new ExpandNodeResponse 
                { 
                    Nodes = immediatePool, 
                    CurrentDistance = currentDistanceFromTarget 
                });
            }

            if (!targetId.HasValue)
            {
                return Results.Ok(new ExpandNodeResponse 
                { 
                    Nodes = rawNeighbors.Take(5).ToList(), 
                    CurrentDistance = null 
                });
            }

            var fastTrackBucket = new List<NodeDto>();
            var detourBucket = new List<NodeDto>();
            var deadEndBucket = new List<NodeDto>();
            NodeDto? immediateClimaxNode = null;

            foreach (var neighbor in rawNeighbors)
            {
                if (neighbor.Id == targetId.Value)
                {
                    neighbor.ConnectionReason = "🎯 TARGET DESTINATION DETECTED! Direct gateway link confirmed.";
                    immediateClimaxNode = neighbor;
                    continue;
                }

                var pathResult = await pathfindingService.FindShortestPathAsync(neighbor.Id, targetId.Value);
                if (pathResult.IsValid)
                {
                    if (currentDistanceFromTarget.HasValue && pathResult.MoveCount < currentDistanceFromTarget.Value)
                    {
                        neighbor.ConnectionReason = "⚡ Fast-Track Link: High relationship synergy brings you closer to destination.";
                        fastTrackBucket.Add(neighbor);
                    }
                    else
                    {
                        neighbor.ConnectionReason = "🔄 Detour Link: Valid connection path, but loops through alternative genre networks.";
                        detourBucket.Add(neighbor);
                    }
                }
                else
                {
                    neighbor.ConnectionReason = "🛑 Dead-End Link: Relationship sequence exists, but breaks trail continuity to target.";
                    deadEndBucket.Add(neighbor);
                }
            }

            var curatedSelection = new List<NodeDto>();

            // 1. Always prioritize the target destination if it's an immediate neighbor
            if (immediateClimaxNode != null)
            {
                curatedSelection.Add(immediateClimaxNode);
            }

            // 2. 🔥 GUARANTEE AT LEAST ONE OPTIMAL PATHWAY (FAST-TRACK) IF IT EXISTS
            if (fastTrackBucket.Any() && immediateClimaxNode == null)
            {
                var guaranteedOptimal = fastTrackBucket.OrderBy(_ => Random.Shared.Next()).First();
                curatedSelection.Add(guaranteedOptimal);
                fastTrackBucket.Remove(guaranteedOptimal); 
            }

            // 3. Gather up alternative route options (other fast tracks or detours)
            var secondaryPathways = fastTrackBucket.Concat(detourBucket).OrderBy(_ => Random.Shared.Next()).ToList();

            // Fill up to 2 total pathway choices to maintain the maze format choice balance
            int targetPathwaysCount = immediateClimaxNode != null ? 1 : Random.Shared.Next(1, 3); 
            while (curatedSelection.Count < targetPathwaysCount && secondaryPathways.Any())
            {
                var extraPath = secondaryPathways.First();
                curatedSelection.Add(extraPath);
                secondaryPathways.Remove(extraPath);
            }

            // 4. Fill the absolute remaining empty grid slots with authentic dead ends to create the challenge
            int remainingSlots = 5 - curatedSelection.Count;
            var randomDistractions = deadEndBucket.OrderBy(_ => Random.Shared.Next()).Take(remainingSlots).ToList();
            curatedSelection.AddRange(randomDistractions);

            // 5. Defensive Edge-Case Check: If total items are still under 5, backfill from whatever is left
            if (curatedSelection.Count < 5)
            {
                int missingCount = 5 - curatedSelection.Count;
                var leftoverPool = rawNeighbors.Except(curatedSelection).OrderBy(_ => Random.Shared.Next()).Take(missingCount);
                curatedSelection.AddRange(leftoverPool);
            }

            // 6. Randomize the final positions so the correct option isn't always sitting in button slot #1
            var shuffledOutput = curatedSelection.OrderBy(_ => Random.Shared.Next()).ToList();

            return Results.Ok(new ExpandNodeResponse
            {
                Nodes = shuffledOutput,
                CurrentDistance = currentDistanceFromTarget
            });
        }); // 👈 FIXED: Properly closes the expand node endpoint lambda expression!

        // 4. 🧭 SMART PATHFINDING ENDPOINT
        group.MapPost("/path/smart", async ([FromBody] FindPathRequest request, IPathfindingService pathfinder) => 
        {
            var result = await pathfinder.FindShortestPathAsync(request.StartNodeId, request.TargetNodeId);
            if (result.IsValid) return Results.Ok(result);
            return Results.NotFound(new { Message = "No path found." });
        });

        // 5. 🛡️ AUTHENTIC PATH VALIDATION ENDPOINT
        group.MapPost("/path/validate", async ([FromBody] ValidatePathRequest request, SongHopDbContext db) => 
        {
            if (request.SubmittedPath == null || request.SubmittedPath.Count < 2)
            {
                return Results.Ok(new { IsValid = false, Message = "A valid path requires a minimum of 2 connected nodes." });
            }

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

public class NodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PopularityScore { get; set; }
    public string? Type { get; set; }
    public string? ConnectionReason { get; set; }
}

public class ExpandNodeResponse
{
    public IEnumerable<NodeDto> Nodes { get; set; } = Array.Empty<NodeDto>();
    public int? CurrentDistance { get; set; }
}

public record FindPathRequest(Guid StartNodeId, Guid TargetNodeId);
public record ValidatePathRequest(List<string> SubmittedPath);