using System.Text;
using SongHop.Core.Models;

public interface IRouteHintService
{
    string GenerateNaturalLanguageHint(Node node);
}

public class RouteHintService : IRouteHintService
{
    private static readonly Dictionary<string, string> CountryOrigins = new(StringComparer.OrdinalIgnoreCase)
    {
        { "US", "American" }, { "GB", "British" }, { "UK", "British" },
        { "CA", "Canadian" }, { "FR", "French" }, { "DE", "German" },
        { "AU", "Australian" }, { "IE", "Irish" }, { "SE", "Swedish" },
        { "JP", "Japanese" }
    };

    public string GenerateNaturalLanguageHint(Node node)
    {
        if (node == null) return "No data available.";

        var parts = new List<string>();

        // 1. Core Identity (e.g., "American solo artist", "French group")
        string origin = !string.IsNullOrWhiteSpace(node.Country) && CountryOrigins.TryGetValue(node.Country, out var descriptiveCountry)
            ? descriptiveCountry
            : "";

        string entityType = "musical act";
        if (!string.IsNullOrWhiteSpace(node.ArtistType))
        {
            entityType = node.ArtistType.Trim().ToLower() switch
            {
                "person" => "",
                "group" => "",
                _ => "project"
            };
        }

        string identity = string.IsNullOrWhiteSpace(origin) ? entityType : $"{origin} {entityType}";
        
        // Append active timeline briefly: "formed in 1993" or "emerged in 2001"
        if (node.StartYear.HasValue)
        {
            string action = (entityType == "group") ? "formed in" : "emerged in";
            identity += $" ({action} {node.StartYear.Value})";
        }
        parts.Add(identity);

        // 2. High-level Styles (e.g., "electronic, house")
        if (!string.IsNullOrWhiteSpace(node.Genres))
        {
            var cleanGenres = string.Join(", ", node.Genres.Split(',').Select(g => g.Trim().ToLower()).Take(1));
            if (!string.IsNullOrWhiteSpace(cleanGenres))
            {
                parts.Add($" {cleanGenres}");
            }
        }

        // Output format example: "French group (formed in 1993) • Styles: electronic, house"
        return string.Join(" • ", parts);
    }
}