namespace SongHop.Core.Models;
public record Edge(
    Guid Id,
    Guid SourceId,
    Guid TargetId,
    EdgeType Type,
    bool IsDirected,     // True for 'RelatedArtist', False for 'Collaboration'
    double BaseWeight    // Dynamic weight to penalize massive hub nodes
);