using Microsoft.EntityFrameworkCore;
using SongHop.Data;
using SongHop.Core.Models;
using SongHop.Core.Interfaces;

namespace SongHop.Pathing.Services;

public class PathfindingService : IPathfindingService
{
    private readonly SongHopDbContext _context;

    public PathfindingService(SongHopDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Finds the shortest path using an optimized bidirectional BFS and wraps it in a PathResult.
    /// </summary>
    public async Task<PathResult> FindShortestPathAsync(Guid startNodeId, Guid targetNodeId)
    {
        if (startNodeId == targetNodeId)
        {
            return new PathResult(new List<Guid> { startNodeId }, 0, 0.0, true);
        }

        var forwardFrontier = new HashSet<Guid> { startNodeId };
        var backwardFrontier = new HashSet<Guid> { targetNodeId };

        var forwardParents = new Dictionary<Guid, Guid> { { startNodeId, Guid.Empty } };
        var backwardParents = new Dictionary<Guid, Guid> { { targetNodeId, Guid.Empty } };

        while (forwardFrontier.Count > 0 && backwardFrontier.Count > 0)
        {
            if (forwardFrontier.Count <= backwardFrontier.Count)
            {
                var meetingNode = await ExpandFrontierAsync(forwardFrontier, forwardParents, backwardParents, isForward: true);
                if (meetingNode.HasValue)
                {
                    var path = ReconstructPath(forwardParents, backwardParents, meetingNode.Value);
                    return new PathResult(path, path.Count - 1, 0.0, true);
                }
            }
            else
            {
                var meetingNode = await ExpandFrontierAsync(backwardFrontier, backwardParents, forwardParents, isForward: false);
                if (meetingNode.HasValue)
                {
                    var path = ReconstructPath(forwardParents, backwardParents, meetingNode.Value);
                    return new PathResult(path, path.Count - 1, 0.0, true);
                }
            }
        }

        // Return an invalid result if no path connects the two nodes
        return new PathResult(new List<Guid>(), 0, 0.0, false);
    }

    // Validates an entire submitted player run in a single efficient database pass.
    public async Task<PathResult> ValidatePathAsync(List<Guid> submittedPath)
    {
        if (submittedPath == null || submittedPath.Count == 0)
        {
            return new PathResult(new List<Guid>(), 0, 0.0, false);
        }

        if (submittedPath.Count == 1)
        {
            bool nodeExists = await _context.Nodes.AnyAsync(n => n.Id == submittedPath[0]);
            return new PathResult(submittedPath, 0, 0.0, nodeExists);
        }

        // 1. Ensure all unique nodes submitted by the player actually exist in the DB
        var uniqueNodes = submittedPath.Distinct().ToList();
        var databaseNodeCount = await _context.Nodes
            .Where(n => uniqueNodes.Contains(n.Id))
            .CountAsync();

        if (databaseNodeCount != uniqueNodes.Count)
        {
            return new PathResult(submittedPath, submittedPath.Count - 1, 0.0, false);
        }

        // 2. Batch-fetch all possible edges connecting these nodes to prevent N+1 queries
        var activeEdges = await _context.Edges
            .Where(e => uniqueNodes.Contains(e.SourceId) && uniqueNodes.Contains(e.TargetId))
            .Select(e => new { e.SourceId, e.TargetId })
            .ToListAsync();

        // 3. Build a fast lookup set containing valid graph directions
        var validConnections = new HashSet<(Guid, Guid)>();
        foreach (var edge in activeEdges)
        {
            validConnections.Add((edge.SourceId, edge.TargetId));
            validConnections.Add((edge.TargetId, edge.SourceId)); // Supporting bidirectional gameplay traversals
        }

        // 4. Trace the player's path step-by-step
        for (int i = 0; i < submittedPath.Count - 1; i++)
        {
            var currentStep = submittedPath[i];
            var nextStep = submittedPath[i + 1];

            if (!validConnections.Contains((currentStep, nextStep)))
            {
                // Cheat detection: A step was taken where no relationship exists!
                return new PathResult(submittedPath, submittedPath.Count - 1, 0.0, false);
            }
        }

        // Run is verified clean!
        return new PathResult(submittedPath, submittedPath.Count - 1, 0.0, true);
    }

    private async Task<Guid?> ExpandFrontierAsync(
        HashSet<Guid> currentFrontier,
        Dictionary<Guid, Guid> currentParents,
        Dictionary<Guid, Guid> oppositeParents,
        bool isForward)
    {
        List<Edge> edges;
        
        if (isForward)
        {
            edges = await _context.Edges
                .Where(e => currentFrontier.Contains(e.SourceId))
                .ToListAsync();
        }
        else
        {
            edges = await _context.Edges
                .Where(e => currentFrontier.Contains(e.TargetId))
                .ToListAsync();
        }

        var nextFrontier = new HashSet<Guid>();

        foreach (var edge in edges)
        {
            Guid source = edge.SourceId;
            Guid target = edge.TargetId;

            Guid current = isForward ? source : target;
            Guid neighbor = isForward ? target : source;

            if (!currentParents.ContainsKey(neighbor))
            {
                currentParents[neighbor] = current;
                nextFrontier.Add(neighbor);

                if (oppositeParents.ContainsKey(neighbor))
                {
                    return neighbor;
                }
            }
        }

        currentFrontier.Clear();
        foreach (var node in nextFrontier)
        {
            currentFrontier.Add(node);
        }

        return null;
    }

    private List<Guid> ReconstructPath(
        Dictionary<Guid, Guid> forwardParents,
        Dictionary<Guid, Guid> backwardParents,
        Guid meetingNode)
    {
        var path = new List<Guid>();

        Guid current = meetingNode;
        while (current != Guid.Empty)
        {
            path.Insert(0, current);
            current = forwardParents[current];
        }

        current = backwardParents[meetingNode];
        while (current != Guid.Empty)
        {
            path.Add(current);
            current = backwardParents[current];
        }

        return path;
    }
}