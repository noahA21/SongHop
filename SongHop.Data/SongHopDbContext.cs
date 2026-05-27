// server/SongHop.Data/SongHopDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SongHop.Core.Models; // 🚨 Using your exact namespace

public class SongHopDbContext : DbContext
{
    public SongHopDbContext(DbContextOptions<SongHopDbContext> options) : base(options) { }

    public DbSet<Node> Nodes { get; set; }
    public DbSet<Edge> Edges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Configure Node Record
        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(n => n.Id);

            // 🌟 CRITICAL FOR THE ETL CRAWLER: 
            // Enforce that ExternalId must be unique so duplicates are blocked at the database level.
            entity.HasIndex(n => n.ExternalId)
                  .IsUnique();
        });

        // 2. Configure Edge Record
        modelBuilder.Entity<Edge>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Set up the self-referencing relationship using your Guid FKs
            entity.HasOne<Node>()
                  .WithMany()
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevents accidental deletes tearing down the graph

            entity.HasOne<Node>()
                  .WithMany()
                  .HasForeignKey(e => e.TargetId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            // Enforce unique edges so we don't map the same connection twice
            entity.HasIndex(e => new { e.SourceId, e.TargetId })
                  .IsUnique();
        });
    }
}