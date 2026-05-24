using SongHop.Core.Models;

namespace SongHop.Core.Interfaces;

public record PathResult(
    List NodeIds, 
    int MoveCount, 
    double RarityScore, 
    bool IsValid
);

public interface IPathfindingService
{
    // Validates a player's submitted run asynchronously
    //Task ValidatePathAsync(List submittedPath);

    // Used by the background worker to find the 'Goldilocks' daily challenges
    //Task FindShortestPathAsync(Guid startNodeId, Guid targetNodeId);
}