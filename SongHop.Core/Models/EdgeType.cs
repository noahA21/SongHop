namespace SongHop.Core.Models;
public enum EdgeType
{
    Collaboration,   // Artist <-> Track
    BandMembership,  // Artist <-> Artist
    RelatedArtist,   // Artist -> Artist (Directed)
    AlbumTrack       // Album <-> Track
}