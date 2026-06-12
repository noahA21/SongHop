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

public static class GameEndpoints{
    public static void MapGameEndpoints(this IEndpointRouteBuilder app){
        var group = app.MapGroup("/v1")
                       .RequireCors("AllowAngularDev")
                       .WithTags("Game Engine");

        // ARTIST SEARCH
        group.MapGet("/node/search", async ([FromQuery] string q, SongHopDbContext db) =>{
            if (string.IsNullOrWhiteSpace(q)) {
                return Results.Ok(new List<Node>());
            }

            var results = await db.Nodes
                .AsNoTracking()
                .Where(n => EF.Functions.ILike(n.Name, $"%{q}%"))
                .Take(15)
                .ToListAsync();

            return Results.Ok(results);
        });

        // WINNABLE SESSION GENERATOR
        group.MapGet("/game/start", async (SongHopDbContext db, IPathfindingService pathfinder) =>{
            var nodes = await db.Nodes.AsNoTracking().ToListAsync();
            if (nodes.Count < 2) return Results.BadRequest("Insufficient node data in database.");

            var random = new Random();
            int maxAttempts = 100;
            int attempt = 0;

            while (attempt < maxAttempts){
                var startNode = nodes[random.Next(nodes.Count)];
                var targetNode = nodes[random.Next(nodes.Count)];

                if (startNode.Id == targetNode.Id) continue;

                var pathResult = await pathfinder.FindShortestPathAsync(startNode.Id, targetNode.Id);

                if (pathResult.IsValid && pathResult.MoveCount >= 2 && pathResult.MoveCount <= 5){
                    return Results.Ok(new{
                        StartNode = startNode,
                        TargetNode = targetNode,
                        OptimalHops = pathResult.MoveCount
                    });
                }
                attempt++;
            }

            foreach (var startNode in nodes.OrderBy(_ => random.Next()).Take(10)){
                foreach (var targetNode in nodes.Where(n => n.Id != startNode.Id).OrderBy(_ => random.Next()).Take(10)){
                    var pathResult = await pathfinder.FindShortestPathAsync(startNode.Id, targetNode.Id);
                    if (pathResult.IsValid){
                        return Results.Ok(new { StartNode = startNode, TargetNode = targetNode, OptimalHops = pathResult.MoveCount });
                    }
                }
            }

            return Results.BadRequest("Could not isolate a connected pair of artists. Ingest more edges via SongHopCrawler.");
        });

        // BIDIRECTIONAL NEIGHBOR EXPANSION WITH DYNAMIC LINER NOTES
        group.MapGet("/node/expand/{id}", async (
            Guid id, 
            [FromQuery] Guid? targetId,
            [FromQuery(Name = "visited")] Guid[]? visited, 
            [FromServices] SongHopDbContext db,
            [FromServices] IPathfindingService pathfindingService) =>{
            var originNode = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);

            var connectedEdgesQuery = db.Edges
                .AsNoTracking()
                .Where(e => e.SourceId == id || e.TargetId == id);

            var connectedEdges = await connectedEdgesQuery.ToListAsync();
            
            var connectedIds = connectedEdges
                .Select(e => e.SourceId == id ? e.TargetId : e.SourceId)
                .Distinct()
                .ToList();

            // EXCLUDE VISITED NODES
            if (visited != null && visited.Any()){
                connectedIds = connectedIds.Where(connectedId => !visited.Contains(connectedId)).ToList();
            }

            var rawNodes = await db.Nodes
                .AsNoTracking()
                .Where(n => connectedIds.Contains(n.Id))
                .ToListAsync();

            var rawNeighbors = rawNodes.Select(n => new NodeDto { 
                Id = n.Id, 
                Name = n.Name, 
                PopularityScore = n.PopularityScore,
                Type = n.Type.ToString(),
                Country = n.Country,
                ArtistType = n.ArtistType,
                StartYear = n.StartYear,
                EndYear = n.EndYear,
                ConnectionReason = string.Empty,
                RouteHint = string.Empty 
            }).ToList();

            if (!rawNeighbors.Any()){
                return Results.Ok(new ExpandNodeResponse { Nodes = new List<NodeDto>(), CurrentDistance = null });
            }

            int? currentDistanceFromTarget = null;
            if (targetId.HasValue){
                var currentPathEvaluation = await pathfindingService.FindShortestPathAsync(id, targetId.Value);
                if (currentPathEvaluation.IsValid){
                    currentDistanceFromTarget = currentPathEvaluation.MoveCount;
                }
            }

            int seed = BitConverter.ToInt32(id.ToByteArray(), 0);
            if (targetId.HasValue) {
                seed ^= BitConverter.ToInt32(targetId.Value.ToByteArray(), 0);
            }
            var deterministicRandom = new Random(seed);

            if (rawNeighbors.Count <= 5){
                var immediatePool = rawNeighbors.OrderBy(_ => deterministicRandom.Next()).ToList();
                return Results.Ok(new ExpandNodeResponse 
                { 
                    Nodes = immediatePool, 
                    CurrentDistance = currentDistanceFromTarget 
                });
            }

            if (!targetId.HasValue){
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

            foreach (var neighbor in rawNeighbors){
                // Pull matching edge metadata to isolate the precise collaboration reason
                var edge = connectedEdges.FirstOrDefault(e => 
                    (e.SourceId == id && e.TargetId == neighbor.Id) || 
                    (e.SourceId == neighbor.Id && e.TargetId == id));

                var neighborNodeType = rawNodes.First(n => n.Id == neighbor.Id).Type;

                string historicalContext = "Connected via shared music performance networks.";
                if (originNode != null)
                {
                    // 🌟 REWRITE: Map specific lineage based on Node Types and Edge Relationships
                    if (originNode.Type == NodeType.Artist && neighborNodeType == NodeType.Track)
                    {
                        historicalContext = $"{originNode.Name} performed, recorded, or collaborated on the track '{neighbor.Name}'.";
                    }
                    else if (originNode.Type == NodeType.Track && neighborNodeType == NodeType.Artist)
                    {
                        historicalContext = $"{neighbor.Name} is credited as a main contributor or performer on the track '{originNode.Name}'.";
                    }
                    else if (originNode.Type == NodeType.Artist && neighborNodeType == NodeType.Album)
                    {
                        historicalContext = $"{originNode.Name} released the studio project or album '{neighbor.Name}'.";
                    }
                    else if (originNode.Type == NodeType.Album && neighborNodeType == NodeType.Artist)
                    {
                        historicalContext = $"The album '{originNode.Name}' was written, produced, or released by {neighbor.Name}.";
                    }
                    else if (originNode.Type == NodeType.Album && neighborNodeType == NodeType.Track)
                    {
                        historicalContext = $"The track '{neighbor.Name}' is featured directly on the album tracklist for '{originNode.Name}'.";
                    }
                    else if (originNode.Type == NodeType.Track && neighborNodeType == NodeType.Album)
                    {
                        historicalContext = $"This track is a key part of the official track selection for the album '{neighbor.Name}'.";
                    }
                    else if (originNode.Type == NodeType.Artist && neighborNodeType == NodeType.Artist && edge != null)
                    {
                        if (edge.Type == EdgeType.BandMembership)
                            historicalContext = $"{originNode.Name} and {neighbor.Name} share historical lineup connections or band memberships.";
                        else if (edge.Type == EdgeType.RelatedArtist)
                            historicalContext = $"{originNode.Name} and {neighbor.Name} share close artistic influences, styles, or musical associations.";
                    }
                }

                neighbor.ConnectionReason = historicalContext;

                if (neighbor.Id == targetId.Value){
                    neighbor.RouteHint = "🎯 Target Destination!";
                    immediateClimaxNode = neighbor;
                    continue;
                }

                var pathResult = await pathfindingService.FindShortestPathAsync(neighbor.Id, targetId.Value);
                
                if (pathResult.IsValid){
                    if (currentDistanceFromTarget.HasValue && pathResult.MoveCount < currentDistanceFromTarget.Value){
                        neighbor.RouteHint = "📈 Closer to Target";
                        fastTrackBucket.Add(neighbor);
                    }
                    else
                    {
                        neighbor.RouteHint = "🔄 Detour";
                        detourBucket.Add(neighbor);
                    }
                }
                else{
                    neighbor.RouteHint = "🛑 Dead End";
                    deadEndBucket.Add(neighbor);
                }
            }

            var curatedSelection = new List<NodeDto>();

            if (immediateClimaxNode != null) curatedSelection.Add(immediateClimaxNode);

            if (fastTrackBucket.Any() && immediateClimaxNode == null){
                var guaranteedOptimal = fastTrackBucket.OrderBy(_ => deterministicRandom.Next()).First();
                curatedSelection.Add(guaranteedOptimal);
                fastTrackBucket.Remove(guaranteedOptimal); 
            }

            var secondaryPathways = fastTrackBucket.Concat(detourBucket).OrderBy(_ => deterministicRandom.Next()).ToList();
            int targetPathwaysCount = immediateClimaxNode != null ? 1 : deterministicRandom.Next(1, 3); 
            
            while (curatedSelection.Count < targetPathwaysCount && secondaryPathways.Any()){
                var extraPath = secondaryPathways.First();
                curatedSelection.Add(extraPath);
                secondaryPathways.Remove(extraPath);
            }

            int remainingSlots = 5 - curatedSelection.Count;
            var randomDistractions = deadEndBucket.OrderBy(_ => deterministicRandom.Next()).Take(remainingSlots).ToList();
            curatedSelection.AddRange(randomDistractions);

            if (curatedSelection.Count < 5){
                int missingCount = 5 - curatedSelection.Count;
                var leftoverPool = rawNeighbors.Except(curatedSelection).OrderBy(_ => deterministicRandom.Next()).Take(missingCount);
                curatedSelection.AddRange(leftoverPool);
            }

            var shuffledOutput = curatedSelection.OrderBy(_ => deterministicRandom.Next()).ToList();

            return Results.Ok(new ExpandNodeResponse{
                Nodes = shuffledOutput,
                CurrentDistance = currentDistanceFromTarget
            });
        });

        // SMART PATHFINDING ENDPOINT
        group.MapPost("/path/smart", async ([FromBody] FindPathRequest request, IPathfindingService pathfinder) => {
            var result = await pathfinder.FindShortestPathAsync(request.StartNodeId, request.TargetNodeId);
            if (result.IsValid) return Results.Ok(result);
            return Results.NotFound(new { Message = "No path found." });
        });

        // AUTHENTIC PATH VALIDATION & STORYBOARD TRAIL ENGINE
        group.MapPost("/path/validate", async ([FromBody] ValidatePathRequest request, SongHopDbContext db) => 
        {
            if (request.SubmittedPath == null || request.SubmittedPath.Count < 2){
                return Results.Ok(new { 
                    IsValid = false, 
                    Message = "A valid path requires a minimum of 2 connected nodes.",
                    LineageTrail = Array.Empty<NodeDto>()
                });
            }

            var parsedIds = new List<Guid>();
            foreach (var stringId in request.SubmittedPath)
            {
                if (!Guid.TryParse(stringId, out Guid nodeGuid))
                {
                    return Results.Ok(new { 
                        IsValid = false, 
                        Message = "Malformed graph node identifiers encountered.",
                        LineageTrail = Array.Empty<NodeDto>()
                    });
                }
                parsedIds.Add(nodeGuid);
            }

            // Verify sequential edges across the trail path
            for (int i = 0; i < parsedIds.Count - 1; i++)
            {
                Guid currentId = parsedIds[i];
                Guid nextId = parsedIds[i + 1];

                bool linkExists = await db.Edges.AsNoTracking().AnyAsync(e => 
                    (e.SourceId == currentId && e.TargetId == nextId) || 
                    (e.SourceId == nextId && e.TargetId == currentId));

                if (!linkExists){
                    return Results.Ok(new { 
                        IsValid = false, 
                        Message = "Verification failed. Broken link sequence detected.",
                        LineageTrail = Array.Empty<NodeDto>()
                    });
                }
            }

            // Batch-fetch nodes and related linking edges across the submitted timeline path
            var nodesMap = await db.Nodes
                .AsNoTracking()
                .Where(n => parsedIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id);

            var structuralEdges = await db.Edges
                .AsNoTracking()
                .Where(e => parsedIds.Contains(e.SourceId) && parsedIds.Contains(e.TargetId))
                .ToListAsync();

            var enrichedTrail = new List<NodeDto>();
            Node? previousNode = null;

            for (int i = 0; i < parsedIds.Count; i++)
            {
                Guid targetId = parsedIds[i];
                if (!nodesMap.TryGetValue(targetId, out var currentNode))
                {
                    return Results.Ok(new {
                        IsValid = false,
                        Message = "Verification aborted. A sequence node missing from records.",
                        LineageTrail = Array.Empty<NodeDto>()
                    });
                }

                string narrativeContext;
                if (i == 0)
                {
                    narrativeContext = "Starting Point";
                }
                else
                {
                    var edge = structuralEdges.FirstOrDefault(e => 
                        (e.SourceId == previousNode!.Id && e.TargetId == currentNode.Id) || 
                        (e.SourceId == currentNode.Id && e.TargetId == previousNode!.Id));

                    narrativeContext = "Connected via mutual performance or production associations.";
                    if (previousNode != null)
                    {
                        // 🌟 REWRITE: Generate concrete descriptions on the victory screen
                        if (previousNode.Type == NodeType.Artist && currentNode.Type == NodeType.Track)
                        {
                            narrativeContext = $"{previousNode.Name} performed, recorded, or collaborated on the track '{currentNode.Name}'.";
                        }
                        else if (previousNode.Type == NodeType.Track && currentNode.Type == NodeType.Artist)
                        {
                            narrativeContext = $"{currentNode.Name} is credited as a main contributor or performer on the track '{previousNode.Name}'.";
                        }
                        else if (previousNode.Type == NodeType.Artist && currentNode.Type == NodeType.Album)
                        {
                            narrativeContext = $"{previousNode.Name} released the studio project or album '{currentNode.Name}'.";
                        }
                        else if (previousNode.Type == NodeType.Album && currentNode.Type == NodeType.Artist)
                        {
                            narrativeContext = $"The album '{previousNode.Name}' was written, produced, or released by {currentNode.Name}.";
                        }
                        else if (previousNode.Type == NodeType.Album && currentNode.Type == NodeType.Track)
                        {
                            narrativeContext = $"The track '{currentNode.Name}' is featured directly on the album tracklist for '{previousNode.Name}'.";
                        }
                        else if (previousNode.Type == NodeType.Track && currentNode.Type == NodeType.Album)
                        {
                            narrativeContext = $"This track is a key part of the official track selection for the album '{currentNode.Name}'.";
                        }
                        else if (previousNode.Type == NodeType.Artist && currentNode.Type == NodeType.Artist && edge != null)
                        {
                            if (edge.Type == EdgeType.BandMembership)
                                narrativeContext = $"{previousNode.Name} and {currentNode.Name} share historical lineup connections or band memberships.";
                            else if (edge.Type == EdgeType.RelatedArtist)
                                narrativeContext = $"{previousNode.Name} and {currentNode.Name} share close artistic influences, styles, or musical associations.";
                        }
                    }
                }

                enrichedTrail.Add(new NodeDto
                {
                    Id = currentNode.Id,
                    Name = currentNode.Name,
                    PopularityScore = currentNode.PopularityScore,
                    Type = currentNode.Type.ToString(),
                    Country = currentNode.Country,
                    ArtistType = currentNode.ArtistType,
                    StartYear = currentNode.StartYear,
                    EndYear = currentNode.EndYear,
                    ConnectionReason = narrativeContext,
                    RouteHint = string.Empty
                });

                previousNode = currentNode;
            }

            return Results.Ok(new { 
                IsValid = true, 
                MoveCount = enrichedTrail.Count - 1,
                Message = "Path verified! Victory confirmed.",
                LineageTrail = enrichedTrail
            });
        });

        // DATA BASING CHECK ENDPOINT
        group.MapGet("/test/nodes", async (SongHopDbContext db) => {
            return Results.Ok(await db.Nodes.AsNoTracking().ToListAsync());
        });
    }
}

public class NodeDto{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PopularityScore { get; set; }
    public string? Type { get; set; }
    public string? Country { get; set; }
    public string? ArtistType { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
    public string? ConnectionReason { get; set; }
    public string? RouteHint { get; set; }
}

public class ExpandNodeResponse{
    public IEnumerable<NodeDto> Nodes { get; set; } = Array.Empty<NodeDto>();
    public int? CurrentDistance { get; set; }
}

public record FindPathRequest(Guid StartNodeId, Guid TargetNodeId);
public record ValidatePathRequest(List<string> SubmittedPath);