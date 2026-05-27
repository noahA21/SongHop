// etl/SongHopCrawler/Services/LastFmClientService.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
public class LastFmClientService
{
    private readonly HttpClient _httpClient;
    // 💡 obfuscate
    private readonly string _apiKey;

    public LastFmClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["LastFm:ApiKey"] 
            ?? throw new InvalidOperationException("Critical: Last.fm ApiKey is missing from configuration files.");
    }

    /// <summary>
    /// Fetches up to 20 highly related artists using Last.fm's similarity graph.
    /// </summary>
    public async Task<List<LastFmArtistDto>> GetRelatedArtistsAsync(string artistName)
    {
        var encodedName = Uri.EscapeDataString(artistName);
        var url = $"http://ws.audioscrobbler.com/2.0/?method=artist.getsimilar&artist={encodedName}&api_key={_apiKey}&format=json&limit=20";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<LastFmResponse>(url);
            return response?.SimilarArtists?.Artist ?? new List<LastFmArtistDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Network error fetching relations for '{artistName}': {ex.Message}");
            return new List<LastFmArtistDto>();
        }
    }
}

// --- Strictly typed DTOs mapping Last.fm's nested JSON responses ---
public record LastFmResponse(
    [property: JsonPropertyName("similarartists")] SimilarArtistsContainer SimilarArtists
);

public record SimilarArtistsContainer(
    [property: JsonPropertyName("artist")] List<LastFmArtistDto> Artist
);

public record LastFmArtistDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mbid")] string? Mbid, // The MusicBrainz identifier
    [property: JsonPropertyName("image")] List<LastFmImageDto>? Image
);

public record LastFmImageDto(
    [property: JsonPropertyName("#text")] string Url,
    [property: JsonPropertyName("size")] string Size
);