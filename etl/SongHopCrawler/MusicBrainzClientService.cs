// etl/SongHopCrawler/MusicBrainzClientService.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

public class MusicBrainzClientService
{
    private readonly HttpClient _httpClient;

    public MusicBrainzClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        
        // MusicBrainz explicitly requires a descriptive User-Agent string with contact info
        var contactEmail = configuration["MusicBrainz:ContactEmail"] 
            ?? throw new InvalidOperationException("Critical: MusicBrainz:ContactEmail is missing from configuration.");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"SongHop/1.0.0 ({contactEmail})");
        _httpClient.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
    }

    public async Task<MusicBrainzArtistDto?> GetArtistMetadataAsync(string mbid)
    {
        if (string.IsNullOrWhiteSpace(mbid)) return null;

        try
        {
            // We append fmt=json because MusicBrainz defaults to XML layout
            var url = $"artist/{mbid}?fmt=json";
            var response = await _httpClient.GetFromJsonAsync<MusicBrainzArtistDto>(url);
            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            Console.WriteLine("⚠️ MusicBrainz Rate Limit hit (503). Backoff required.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Network error fetching MusicBrainz details for MBID '{mbid}': {ex.Message}");
            return null;
        }
    }
}

#region Data Transfer Objects (DTOs)

public record MusicBrainzArtistDto(
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("type")] string? ArtistType,
    [property: JsonPropertyName("life-span")] LifeSpanDto? LifeSpan
);

public record LifeSpanDto(
    [property: JsonPropertyName("begin")] string? Begin, // Holds dates like "1993" or "1993-05-12"
    [property: JsonPropertyName("end")] string? End
);

#endregion