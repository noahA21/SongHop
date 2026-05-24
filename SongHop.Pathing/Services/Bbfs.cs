// using SongHop.Core.Interfaces;
// using SongHop.Core.Models;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;

namespace SongHop.Pathing.Services;

public class PathfindingService : IPathfindingService
{
    private readonly IGraphRepository _graphRepository;
    
    // Hard limit to prevent infinite loops if two nodes are completely disconnected
    private const int MaxSearchDepth = 10; 

    public PathfindingService(IGraphRepository graphRepository)
    {
        _graphRepository = graphRepository;
    }

    public async Task<PathResult> FindShortestPathAsync(Guid startNodeId, Guid targetNodeId)
    {
        if (startNodeId == targetNodeId)
        {
            return new PathResult(new List<Guid> { startNodeId }, 0, 0, true);
        }

        var forwardQueue = new Queue<Guid>();
        var backwardQueue = new Queue<Guid>();

        // Tracks visited nodes and maps NodeId -> ParentNodeId for path reconstruction
        var forwardVisited = new Dictionary<Guid, Guid>();
        var backwardVisited = new Dictionary<Guid, Guid>();

        forwardQueue.Enqueue(startNodeId);
        forwardVisited[startNodeId] = Guid.Empty;

        backwardQueue.Enqueue(targetNodeId);
        backwardVisited[targetNodeId] = Guid.Empty;

        Guid intersectionNode = Guid.Empty;
        int currentDepth = 0;

        while (forwardQueue.Count > 0 && backwardQueue.Count > 0 && currentDepth < MaxSearchDepth)
        {
            // Optimization: Always expand the smaller frontier first to save memory and time
            if (forwardQueue.Count <= backwardQueue.Count)
            {
                intersectionNode = await ExpandFrontierAsync(forwardQueue, forwardVisited, backwardVisited, isForwardSearch: true);
            }
            else
            {
                intersectionNode = await ExpandFrontierAsync(backwardQueue, backwardVisited, forwardVisited, isForwardSearch: false);
            }

            if (intersectionNode != Guid.Empty)
            {
                var path = ReconstructPath(forwardVisited, backwardVisited, intersectionNode);
                double rarityScore = await CalculateRarityScoreAsync(path);
                return new PathResult(path, path.Count - 1, rarityScore, true);
            }

            currentDepth++;
        }

        // No path found within depth limit
        return new PathResult(new List<Guid>(), 0, 0, false);
    }

    public async Task<PathResult> ValidatePathAsync(List<Guid> submittedPath)
    {
        if (submittedPath == null || submittedPath.Count < 2)
            return new PathResult(submittedPath ?? new List<Guid>(), 0, 0, false);

        for (int i = 0; i < submittedPath.Count - 1; i++)
        {
            var current = submittedPath[i];
            var next = submittedPath[i + 1];
            var edges = await _graphRepository.GetNeighborsAsync(current);
            
            // Validate the move. Handles both undirected (Collaborations) and directed (Related Artists) edges.
            bool isValidMove = edges.Any(e => 
                (e.SourceId == current && e.TargetId == next) || 
                (!e.IsDirected && e.SourceId == next && e.TargetId == current));

            if (!isValidMove) 
                return new PathResult(submittedPath, 0, 0, false);
        }

        double rarityScore = await CalculateRarityScoreAsync(submittedPath);
        return new PathResult(submittedPath, submittedPath.Count - 1, rarityScore, true);
    }

    /// <summary>
    /// Expands the current queue by one level and checks for intersections with the opposite search frontier.
    /// </summary>
    private async Task<Guid> ExpandFrontierAsync(
        Queue<Guid> queue, 
        Dictionary<Guid, Guid> currentVisited, 
        Dictionary<Guid, Guid> oppositeVisited,
        bool isForwardSearch)
    {
        int levelSize = queue.Count;

        for (int i = 0; i < levelSize; i++)
        {
            var currentNode = queue.Dequeue();
            var edges = await _graphRepository.GetNeighborsAsync(currentNode);

            foreach (var edge in edges)
            {
                // Determine the neighbor based on search direction and edge directionality
                Guid neighborId = GetValidNeighbor(currentNode, edge, isForwardSearch);
                if (neighborId == Guid.Empty) continue;

                // Intersection found! The two searches have met.
                if (oppositeVisited.ContainsKey(neighborId))
                {
                    currentVisited[neighborId] = currentNode;
                    return neighborId;
                }

                // If neighbor hasn't been visited in the current direction, enqueue it
                if (!currentVisited.ContainsKey(neighborId))
                {
                    currentVisited[neighborId] = currentNode;
                    queue.Enqueue(neighborId);
                }
            }
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Handles the complex logic of one-way streets (Directed Edges) during a reverse search.
    /// </summary>
    private Guid GetValidNeighbor(Guid currentNode, Edge edge, bool isForwardSearch)
    {
        if (isForwardSearch)
        {
            // Moving forward: If we are the source, target is neighbor. 
            // If we are target AND edge is undirected, source is neighbor.
            if (edge.SourceId == currentNode) return edge.TargetId;
            if (!edge.IsDirected && edge.TargetId == currentNode) return edge.SourceId;
        }
        else
        {
            // Moving backward: If edge is directed, we MUST be traversing from Target to Source.
            if (edge.IsDirected)
            {
                if (edge.TargetId == currentNode) return edge.SourceId;
            }
            else
            {
                // Undirected edge (e.g., Collab): Treat it purely as bidirectional
                return edge.SourceId == currentNode ? edge.TargetId : edge.SourceId;
            }
        }

        return Guid.Empty; // Invalid traversal path
    }

    private List<Guid> ReconstructPath(
        Dictionary<Guid, Guid> forwardVisited, 
        Dictionary<Guid, Guid> backwardVisited, 
        Guid intersectionNode)
    {
        var path = new List<Guid>();

        // Build forward path from start to intersection
        var current = intersectionNode;
        while (current != Guid.Empty)
        {
            path.Add(current);
            current = forwardVisited[current];
        }
        path.Reverse(); // Now ordered: Start -> Intersection

        // Build backward path from intersection to target
        current = backwardVisited[intersectionNode];
        while (current != Guid.Empty)
        {
            path.Add(current);
            current = backwardVisited[current];
        }
        // No reverse needed here, appending logic handles Target orientation natively

        return path;
    }

    private async Task<double> CalculateRarityScoreAsync(List<Guid> path)
    {
        var nodes = await _graphRepository.GetNodesByIdsAsync(path);
        
        // MVP Inverse Popularity Scoring: 
        // Highly popular bridging nodes (Spotify score 90+) yield very few points.
        // Obscure bridging nodes (Spotify score 10) yield massive multiplier points.
        double score = 0;
        foreach (var node in nodes)
        {
            // Prevent division by zero and cap maximum reward
            int safePopularity = Math.Max(node.PopularityScore, 1);
            score += (100.0 / safePopularity) * 10; 
        }

        return Math.Round(score, 2);
    }
}