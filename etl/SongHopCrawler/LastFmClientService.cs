// etl/SongHopCrawler/Services/LastFmClientService.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

public class LastFmClientService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public LastFmClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["LastFm:ApiKey"] 
            ?? throw new InvalidOperationException("Critical: Last.fm ApiKey is missing from configuration files.");
    }

    // Fetches up to 20 highly related artists using Last.fm's similarity graph.
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

    // 🌟 NEW: Fetches top community tags for an artist to generate human-readable route hints
    public async Task<List<LastFmTagDto>> GetTopTagsAsync(string artistName)
    {
        var encodedName = Uri.EscapeDataString(artistName);
        var url = $"http://ws.audioscrobbler.com/2.0/?method=artist.gettoptags&artist={encodedName}&api_key={_apiKey}&format=json";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<LastFmTopTagsResponse>(url);
            return response?.TopTags?.Tag ?? new List<LastFmTagDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Network error fetching tags for '{artistName}': {ex.Message}");
            return new List<LastFmTagDto>();
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
    [property: JsonPropertyName("mbid")] string? Mbid, 
    [property: JsonPropertyName("image")] List<LastFmImageDto>? Image
);

public record LastFmImageDto(
    [property: JsonPropertyName("#text")] string Url,
    [property: JsonPropertyName("size")] string Size
);

// 🌟 NEW DTOs FOR GENRE INGESTION
public record LastFmTopTagsResponse(
    [property: JsonPropertyName("toptags")] TopTagsContainer TopTags
);

public record TopTagsContainer(
    [property: JsonPropertyName("tag")] List<LastFmTagDto> Tag
);

public record LastFmTagDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("count")] int Count
);