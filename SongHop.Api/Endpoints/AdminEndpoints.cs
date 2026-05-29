using Microsoft.EntityFrameworkCore;
using SongHop.Core.Models;
using SongHop.Data;

namespace SongHop.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin");

        // group.MapPost("/seed", async (SongHopDbContext db) =>
        // {
        //     if (await db.Nodes.AnyAsync())
        //     {
        //         return Results.Conflict("Database already has data.");
        //     }

        //     var artist1 = new Node(Guid.NewGuid(), "Daft Punk", NodeType.Artist, 100, null, null);
        //     var artist2 = new Node(Guid.NewGuid(), "Pharrell Williams", NodeType.Artist, 95, null, null);
        //     var artist3 = new Node(Guid.NewGuid(), "Nile Rodgers", NodeType.Artist, 90, null, null);

        //     await db.Nodes.AddRangeAsync(artist1, artist2, artist3);

        //     var edges = new List<Edge>
        //     {
        //         new Edge(Guid.NewGuid(), artist1.Id, artist2.Id, EdgeType.RelatedArtist, true, 42),
        //         new Edge(Guid.NewGuid(), artist1.Id, artist3.Id, EdgeType.RelatedArtist, true, 65),
        //         new Edge(Guid.NewGuid(), artist2.Id, artist3.Id, EdgeType.RelatedArtist, true, 32)
        //     };

        //     await db.Edges.AddRangeAsync(edges);
        //     await db.SaveChangesAsync();

        //     return Results.Ok(new { Message = "Seed successful" });
        // });
    }
}