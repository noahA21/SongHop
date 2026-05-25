// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.EntityFrameworkCore;
// using SongHop.Core.Interfaces;
// using SongHop.Core.Models;

// namespace SongHop.Data;

// public class GraphRepository : IGraphRepository
// {
//     private readonly SongHopDbContext _context;

//     public GraphRepository(SongHopDbContext context)
//     {
//         _context = context;
//     }

//     // Fetches only immediate connections (edges) for a node during guided exploration
//     public async Task<IEnumerable<Edge>> GetNeighborsAsync(Guid nodeId)
//     {
//         return await _context.Edges
//             .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
//             .ToListAsync();
//     }

//     // Batch fetches node details for rendering the final path summaries
//     public async Task<IEnumerable<Node>> GetNodesByIdsAsync(IEnumerable<Guid> nodeIds)
//     {
//         return await _context.Nodes
//             .Where(n => nodeIds.Contains(n.Id))
//             .ToListAsync();
//     }
// }