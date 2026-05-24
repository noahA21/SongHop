namespace SongHop.Core.Models;

public record Node(
    Guid Id,
    string Name,
    NodeType Type,
    int PopularityScore, // 0-100 from Spotify
    string? ImageUrl,    // For UI disambiguation
    string? SpotifyId    // Foreign key link back to source
);
