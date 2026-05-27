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
    // NEW MUSICBRAINZ METADATA FIELDS
    // Added inside the body so we don't break your existing 'new Node(...)' calls.
    // Using { get; set; } makes it incredibly easy for the enrichment service to update them.
    public string? Country { get; set; }       // e.g., "FR", "US", "GB"
    public string? ArtistType { get; set; }    // e.g., "Group", "Person", "Orchestra"
    public int? StartYear { get; set; }        // Year formed or born
    public int? EndYear { get; set; }          // Year disbanded (null if still active)
}