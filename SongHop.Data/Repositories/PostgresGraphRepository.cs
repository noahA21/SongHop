// SongHop.Data/Repositories/PostgresGraphRepository.cs
using Microsoft.EntityFrameworkCore;
using SongHop.Core.Interfaces;
using SongHop.Core.Models;

namespace SongHop.Data.Repositories;

public class PostgresGraphRepository : IGraphRepository
{
    private readonly SongHopDbContext _context;

    public PostgresGraphRepository(SongHopDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Edge>> GetNeighborsAsync(Guid nodeId)
    {
        // Fetch all edges radiating OUT from this node
        return await _context.Edges
            .Where(e => e.SourceId == nodeId)
            .AsNoTracking() // Performance boost: we don't need change tracking for pathfinding
            .ToListAsync();
    }

    public async Task<IEnumerable<Node>> GetNodesByIdsAsync(IEnumerable<Guid> nodeIds)
    {
        var idList = nodeIds.ToList();
        
        // Fetch all matching nodes from the database
        var nodes = await _context.Nodes
            .Where(n => idList.Contains(n.Id))
            .AsNoTracking()
            .ToListAsync();

        // Crucial: EF Core doesn't guarantee the returned array matches the input order.
        // We re-sort them here to preserve the exact sequence of the game path.
        return idList
            .Select(id => nodes.FirstOrDefault(n => n.Id == id))
            .Where(n => n != null)!;
    }
}