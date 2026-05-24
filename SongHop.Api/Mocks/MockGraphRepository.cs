using SongHop.Core.Interfaces;
using SongHop.Core.Models;

namespace SongHop.Api.Mocks;

public class MockGraphRepository : IGraphRepository
{
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();

    public MockGraphRepository()
    {
        // Seed the nodes
        var radiohead = new Node(Guid.NewGuid(), "Radiohead", NodeType.Artist, 85, null, null);
        var thomYorke = new Node(Guid.NewGuid(), "Thom Yorke", NodeType.Artist, 70, null, null);
        var flyingLotus = new Node(Guid.NewGuid(), "Flying Lotus", NodeType.Artist, 60, null, null);
        var kendrickLamar = new Node(Guid.NewGuid(), "Kendrick Lamar", NodeType.Artist, 95, null, null);
        var snoopDogg = new Node(Guid.NewGuid(), "Snoop Dogg", NodeType.Artist, 90, null, null); // Hub trap

        _nodes.AddRange(new[]{ radiohead, thomYorke, flyingLotus, kendrickLamar, snoopDogg });

        // Seed the edges (The winning path: Radiohead -> Thom Yorke -> Flying Lotus -> Kendrick Lamar)
        AddBidirectionalEdge(radiohead.Id, thomYorke.Id, EdgeType.BandMembership);
        AddBidirectionalEdge(thomYorke.Id, flyingLotus.Id, EdgeType.Collaboration);
        AddBidirectionalEdge(flyingLotus.Id, kendrickLamar.Id, EdgeType.Collaboration);
        
        // Add a trap path to test the engine
        AddBidirectionalEdge(radiohead.Id, snoopDogg.Id, EdgeType.Collaboration);
        AddBidirectionalEdge(snoopDogg.Id, kendrickLamar.Id, EdgeType.Collaboration);
    }

    private void AddBidirectionalEdge(Guid source, Guid target, EdgeType type)
    {
        _edges.Add(new Edge(Guid.NewGuid(), source, target, type, false, 1.0));
        _edges.Add(new Edge(Guid.NewGuid(), target, source, type, false, 1.0));
    }

    public Task<IEnumerable<Edge>> GetNeighborsAsync(Guid nodeId)
    {
        var neighbors = _edges.Where(e => e.SourceId == nodeId).ToList();
        return Task.FromResult<IEnumerable<Edge>>(neighbors);
    }

    public Task<IEnumerable<Node>> GetNodesByIdsAsync(IEnumerable<Guid> nodeIds)
    {
        var matchingNodes = _nodes.Where(n => nodeIds.Contains(n.Id)).ToList();
        
        // Ensure nodes are returned in the exact order requested by the path
        var orderedNodes = nodeIds
            .Select(id => matchingNodes.FirstOrDefault(n => n.Id == id))
            .Where(n => n != null)
            .ToList();
            
        return Task.FromResult<IEnumerable<Node>>(orderedNodes!);
    }
    
    // Helper method just for testing to grab the seeded IDs easily
    public List<Node> GetAllNodes() => _nodes;
}