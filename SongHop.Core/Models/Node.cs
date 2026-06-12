namespace SongHop.Core.Models;

public record Node(
    Guid Id,
    string Name,
    NodeType Type,
    int PopularityScore, // 0-100
    string? ImageUrl,    // For UI disambiguation
    string? ExternalId   // Foreign key link back to source (Holds MusicBrainz ID)
)
{
    // EXISTING MUSICBRAINZ METADATA FIELDS
    public string? Country { get; set; }       // e.g., "FR", "US", "GB"
    public string? ArtistType { get; set; }    // e.g., "Group", "Person", "Orchestra"
    public int? StartYear { get; set; }        // Year formed or born
    public int? EndYear { get; set; }          // Year disbanded (null if still active)

    // 🌟 NEW: Top genres/tags for natural language hints
    public string? Genres { get; set; }        // e.g., "electronic, synth-pop, indie rock"
}