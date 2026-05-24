using SongHop.Core.Models;

namespace SongHop.Core.Interfaces;

public interface IGraphRepository
{
    // Fetches only immediate connections for a node during guided exploration
    //Task> GetNeighborsAsync(Guid nodeId);
    
    // Batch fetches node details for rendering the final path summaries
    //Task> GetNodesByIdsAsync(IEnumerable nodeIds);
}