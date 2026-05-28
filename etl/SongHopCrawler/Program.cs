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
    .AddJsonFile("etl/SongHopCrawler/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("etl/SongHopCrawler/appsettings.Development.json", optional: true, reloadOnChange: true)
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

Console.WriteLine("📥 Extracting graph layers from music ecosystem...");

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
        Console.WriteLine($"✅ Mapped batch connections for '{sourceNode.Name}' to Postgres context.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Pipeline block on artist '{currentName}': {ex.Message}");
    }
}

Console.WriteLine("\n🏁 SongHop ETL Pipeline completed successfully!");
Console.WriteLine($"Total processed hub nodes: {processedNodesCount}. Core graph is now populated.");

#endregion
// =========================================================================
// NEW: PHASE 2 - BATCH MUSICBRAINZ ENRICHMENT
// =========================================================================
Console.WriteLine("\n⏳ Transitioning to Phase 2: MusicBrainz Metadata Enrichment...");

// Create a clean scope to pull the DB context and MusicBrainz service
using (var enrichmentScope = serviceProvider.CreateScope())
{
    var dbContext = enrichmentScope.ServiceProvider.GetRequiredService<SongHopDbContext>();
    var mbClient = enrichmentScope.ServiceProvider.GetRequiredService<MusicBrainzClientService>();

    // 1. Query Postgres for nodes that have a MusicBrainz ID but haven't been enriched yet
    var unenrichedNodes = await dbContext.Nodes
        .Where(n => n.ExternalId != null && n.Country == null)
        .ToListAsync();

    Console.WriteLine($"🔍 Found {unenrichedNodes.Count} artists awaiting metadata enrichment.");

    int enrichedCount = 0;

    foreach (var node in unenrichedNodes)
    {
        Console.Write($"📥 Enriching [{node.Name}] ({node.ExternalId})... ");

        // 2. Fetch the metadata from the MusicBrainz API
        var metadata = await mbClient.GetArtistMetadataAsync(node.ExternalId!);

        if (metadata != null)
        {
            // 3. Map the fields over to our mutable properties in the Node record body
            node.Country = metadata.Country;
            node.ArtistType = metadata.ArtistType;
            node.StartYear = ParseYearFromMusicBrainz(metadata.LifeSpan?.Begin);
            node.EndYear = ParseYearFromMusicBrainz(metadata.LifeSpan?.End);

            // 4. Save progress immediately node-by-node. 
            // Because we have a mandatory 1.2 second delay anyway, saving individually 
            // takes milliseconds and guarantees that we don't lose progress if the app is stopped.
            await dbContext.SaveChangesAsync();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("SUCCESS");
            Console.ResetColor();
            enrichedCount++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("SKIPPED (No data found or rate limited)");
            Console.ResetColor();
        }

        // 5. THE ARRESTING THROTTLE: Enforce strict compliance with MusicBrainz's 1-request-per-second rule
        await Task.Delay(1200); 
    }

    Console.WriteLine($"\n🎉 Phase 2 Complete! Successfully enriched {enrichedCount} nodes with structural catalog metadata.");
}

// 💡 Helper function to parse MusicBrainz dates (like "1993-05-12" or "1993") into a clean C# integer
int? ParseYearFromMusicBrainz(string? dateString)
{
    if (string.IsNullOrWhiteSpace(dateString)) return null;
    
    // MusicBrainz dates always start with the 4-digit year
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
    
    // Look up via name or an explicit identifier token
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
    // Build a unique tracking token if MusicBrainz ID is missing to protect the unique index
    string uniqueExternalId = !string.IsNullOrWhiteSpace(dto.Mbid) 
        ? dto.Mbid 
        : $"NAME:{dto.Name.Trim().ToLower()}";

    // Deduplicate at the data entry point
    var existingNode = await context.Nodes.FirstOrDefaultAsync(n => n.ExternalId == uniqueExternalId);
    if (existingNode != null) return existingNode;

    // Grab a high-quality large image URL if present in the collection
    var largeImage = dto.Image?.FirstOrDefault(i => i.Size == "large")?.Url;

    var newNode = new Node(
        Id: Guid.NewGuid(),
        Name: dto.Name,
        Type: NodeType.Artist,
        PopularityScore: 50, // Default baseline for similarity nodes
        ImageUrl: largeImage,
        ExternalId: uniqueExternalId
    );

    await context.Nodes.AddAsync(newNode);
    return newNode;
}

async Task EnsureEdgeExistsAsync(SongHopDbContext context, Guid sourceGuid, Guid targetGuid)
{
    if (sourceGuid == targetGuid) return;

    // Verify a matching edge pair doesn't already link these nodes
    var edgeExists = await context.Edges.AnyAsync(e => e.SourceId == sourceGuid && e.TargetId == targetGuid);
    if (!edgeExists)
    {
        var newEdge = new Edge(
            Id: Guid.NewGuid(),
            SourceId: sourceGuid,
            TargetId: targetGuid,
            Type: EdgeType.RelatedArtist,
            IsDirected: true,
            BaseWeight: 1.0
        );
        await context.Edges.AddAsync(newEdge);
    }
}
#endregion