using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SongHop.Core.Models;

namespace SongHop.Core.Interfaces;

public interface IGraphRepository
{
    // Fetches only immediate connections for a node during guided exploration
    Task<IEnumerable<Edge>> GetNeighborsAsync(Guid nodeId);
    
    // Batch fetches node details for rendering the final path summaries
    Task<IEnumerable<Node>> GetNodesByIdsAsync(IEnumerable<Guid> nodeIds);
}