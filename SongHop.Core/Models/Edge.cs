namespace SongHop.Core.Models;

public record Edge(
    Guid Id,
    Guid SourceId,
    Guid TargetId,
    EdgeType Type,
    bool IsDirected,     // True for 'RelatedArtist', False for 'Collaboration'
    double BaseWeight    // Dynamic weight to penalize massive hub nodes
)
{
    // 🌟 NEW: Lineage metadata for the victory screen
    public string? ContextTitle { get; set; } // e.g., "Get Lucky" (Track name) or "The Neptunes" (Group name)
    public int? ContextYear { get; set; }     // e.g., 2013
    public string? ContextRole { get; set; }     // e.g., "Featured Artist", "Producer", "Band Member"
}