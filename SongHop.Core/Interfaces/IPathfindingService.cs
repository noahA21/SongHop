using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SongHop.Core.Models;

namespace SongHop.Core.Interfaces;

public record PathResult(
    List<Guid> NodeIds, 
    int MoveCount, 
    double RarityScore, 
    bool IsValid
);

public interface IPathfindingService
{
    // Validates a player's submitted run asynchronously
    Task<PathResult> ValidatePathAsync(List<Guid> submittedPath);

    // Used by the background worker to find the optimal path in daily challenges
    Task<PathResult> FindShortestPathAsync(Guid startNodeId, Guid targetNodeId);
}