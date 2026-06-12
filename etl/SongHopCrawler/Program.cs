// etl/SongHopifyCrawler/Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SongHop.Core.Models; 
using SongHop.Data;

Console.WriteLine("🚀 Initializing SongHop Ingestion & Last.fm Graph Crawler Engine...");
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
#region 1. Dependency Injection Setup

// 1. Tell .NET to build the configuration chain
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("'DefaultConnection' string is missing from configuration.");

var services = new ServiceCollection();
services.AddHttpClient();
services.AddDbContext<SongHopDbContext>(options => options.UseNpgsql(connectionString));
services.AddSingleton<IConfiguration>(configuration); 
services.AddSingleton<LastFmClientService>();
services.AddSingleton<MusicBrainzClientService>();

var serviceProvider = services.BuildServiceProvider();

#endregion

#region 2. Crawler Constraints & Seed Selection

const int MAX_DEPTH = 2;        // How many degrees of separation to crawl
const int MAX_NODES_CAP = 250;  // Safety boundary for a single run
int processedNodesCount = 0;

// Seed the network traversal with a major hub artist
string seedArtistName = "Daft Punk";

// Queue holds the artist's name and their current graph depth
var discoveryQueue = new Queue<(string ArtistName, int Depth)>();
var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

discoveryQueue.Enqueue((seedArtistName, 0));

#endregion

#region 3. Traversal Loop

using var scope = serviceProvider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<SongHopDbContext>();
var lastFmClient = scope.ServiceProvider.GetRequiredService<LastFmClientService>();

Console.WriteLine(" Extracting graph layers from music ecosystem...");

while (discoveryQueue.Count > 0 && processedNodesCount < MAX_NODES_CAP)
{
    var (currentName, currentDepth) = discoveryQueue.Dequeue();

    if (processedNames.Contains(currentName)) continue;
    processedNames.Add(currentName);

    if (currentDepth > MAX_DEPTH) continue;

    try
    {
        // 1. Ensure the root source node exists in the database
        var sourceNode = await GetOrCreateRootNodeAsync(db, currentName);
        Console.WriteLine($"\n[Layer {currentDepth}] Crawling connections radiating from: {sourceNode.Name}...");

        // 2. Fetch the 20 most similar nodes from the API
        var relatedArtists = await lastFmClient.GetRelatedArtistsAsync(currentName);
        processedNodesCount++;

        foreach (var artistDto in relatedArtists)
        {
            if (string.IsNullOrWhiteSpace(artistDto.Name)) continue;

            // 3. Insert or locate the neighboring target node
            var targetNode = await UpsertTargetNodeAsync(db, artistDto);

            // 4. Safely create a unique edge between them
            await EnsureEdgeExistsAsync(db, sourceNode.Id, targetNode.Id);

            // 5. Enqueue the target for the next layer if boundaries allow
            if (currentDepth + 1 <= MAX_DEPTH && !processedNames.Contains(artistDto.Name))
            {
                discoveryQueue.Enqueue((artistDto.Name, currentDepth + 1));
            }
        }

        // Persist the batch modifications securely to PostgreSQL
        await db.SaveChangesAsync();
        Console.WriteLine($" Mapped batch connections for '{sourceNode.Name}' to Postgres context.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Pipeline block on artist '{currentName}': {ex.Message}");
    }
}

Console.WriteLine("\n SongHop ETL Pipeline completed successfully!");
Console.WriteLine($"Total processed hub nodes: {processedNodesCount}. Core graph is now populated.");

#endregion
// =========================================================================
// NEW: PHASE 2 - METADATA & LAST.FM GENRE ENRICHMENT (THE BETTER METHOD)
// =========================================================================
Console.WriteLine("\n Transitioning to Phase 2: Metadata & Last.fm Genre Enrichment...");

using (var enrichmentScope = serviceProvider.CreateScope())
{
    var dbContext = enrichmentScope.ServiceProvider.GetRequiredService<SongHopDbContext>();
    var mbClient = enrichmentScope.ServiceProvider.GetRequiredService<MusicBrainzClientService>();
    var lfClient = enrichmentScope.ServiceProvider.GetRequiredService<LastFmClientService>();

    // Locate nodes missing country profile details or structural genre classifications
    var unenrichedNodes = await dbContext.Nodes
        .Where(n => n.Country == null || n.Genres == null)
        .ToListAsync();

    Console.WriteLine($" Found {unenrichedNodes.Count} artists awaiting metadata enrichment.");

    int enrichedCount = 0;

    foreach (var node in unenrichedNodes)
    {
        Console.Write($" Enriching [{node.Name}]... ");
        bool hasChanges = false;

        try
        {
            // 1. Ingest Rich Genre tags via Last.fm API
            if (string.IsNullOrWhiteSpace(node.Genres))
            {
                var topTags = await lfClient.GetTopTagsAsync(node.Name);
                if (topTags != null && topTags.Any())
                {
                    // Filter out descriptive noise, year matches, or identity loops
                    var pureGenres = topTags
                        .Select(t => t.Name.ToLower().Trim())
                        .Where(tag => tag.Length > 2 
                                      && tag != node.Name.ToLower() 
                                      && !tag.Contains("seen live") 
                                      && !tag.Contains("favorite"))
                        .Take(3);

                    if (pureGenres.Any())
                    {
                        node.Genres = string.Join(", ", pureGenres);
                        hasChanges = true;
                    }
                }
            }

            // 2. Ingest Country & Timeline via MusicBrainz API (If valid MBID exists)
            if (node.ExternalId != null && !node.ExternalId.StartsWith("NAME:") && node.Country == null)
            {
                var metadata = await mbClient.GetArtistMetadataAsync(node.ExternalId!);
                if (metadata != null)
                {
                    node.Country = metadata.Country;
                    node.ArtistType = metadata.ArtistType;
                    node.StartYear = ParseYearFromMusicBrainz(metadata.LifeSpan?.Begin);
                    node.EndYear = ParseYearFromMusicBrainz(metadata.LifeSpan?.End);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await dbContext.SaveChangesAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("SUCCESS");
                Console.ResetColor();
                enrichedCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("SKIPPED (No updates found)");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED: {ex.Message}");
            Console.ResetColor();
        }

        // Throttle crawler iterations to comply with traffic regulations safely
        await Task.Delay(1100); 
    }

    Console.WriteLine($"\n Phase 2 Complete! Successfully enriched {enrichedCount} nodes with structural catalog metadata.");
}

int? ParseYearFromMusicBrainz(string? dateString)
{
    if (string.IsNullOrWhiteSpace(dateString)) return null;
    if (dateString.Length >= 4 && int.TryParse(dateString.Substring(0, 4), out int year))
    {
        return year;
    }
    return null;
}

#region 4. Data Mapping & Resilient Upsert Utilities

async Task<Node> GetOrCreateRootNodeAsync(SongHopDbContext context, string name)
{
    var normalizedName = name.Trim().ToLower();
    var node = await context.Nodes.FirstOrDefaultAsync(n => 
        n.Name.ToLower() == normalizedName || n.ExternalId == $"NAME:{normalizedName}");
        
    if (node != null) return node;

    var newNode = new Node(
        Id: Guid.NewGuid(),
        Name: name,
        Type: NodeType.Artist,
        PopularityScore: 80,
        ImageUrl: null,
        ExternalId: $"NAME:{normalizedName}"
    );

    await context.Nodes.AddAsync(newNode);
    await context.SaveChangesAsync();
    return newNode;
}

async Task<Node> UpsertTargetNodeAsync(SongHopDbContext context, LastFmArtistDto dto)
{
    string uniqueExternalId = !string.IsNullOrWhiteSpace(dto.Mbid) 
        ? dto.Mbid 
        : $"NAME:{dto.Name.Trim().ToLower()}";

    var existingNode = await context.Nodes.FirstOrDefaultAsync(n => n.ExternalId == uniqueExternalId);
    if (existingNode != null) return existingNode;

    var largeImage = dto.Image?.FirstOrDefault(i => i.Size == "large")?.Url;

    var newNode = new Node(
        Id: Guid.NewGuid(),
        Name: dto.Name,
        Type: NodeType.Artist,
        PopularityScore: 50, 
        ImageUrl: largeImage,
        ExternalId: uniqueExternalId
    );

    await context.Nodes.AddAsync(newNode);
    return newNode;
}

async Task EnsureEdgeExistsAsync(
    SongHopDbContext context, 
    Guid sourceGuid, 
    Guid targetGuid,
    EdgeType type = EdgeType.RelatedArtist,
    string? contextTitle = null,
    int? contextYear = null,
    string? contextRole = null)
{
    if (sourceGuid == targetGuid) return;

    var edgeExists = await context.Edges.AnyAsync(e => 
        e.SourceId == sourceGuid && 
        e.TargetId == targetGuid && 
        e.Type == type && 
        e.ContextTitle == contextTitle);

    if (!edgeExists)
    {
        var newEdge = new Edge(
            Id: Guid.NewGuid(),
            SourceId: sourceGuid,
            TargetId: targetGuid,
            Type: type,
            IsDirected: type == EdgeType.RelatedArtist,
            BaseWeight: 1.0
        )
        {
            ContextTitle = contextTitle,
            ContextYear = contextYear,
            ContextRole = contextRole
        };
        await context.Edges.AddAsync(newEdge);
    }
}
#endregion