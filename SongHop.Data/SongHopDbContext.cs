// SongHop.Data/SongHopDbContext.cs
using Microsoft.EntityFrameworkCore;
using SongHop.Core.Models;

namespace SongHop.Data;

public class SongHopDbContext : DbContext
{
    public SongHopDbContext(DbContextOptions<SongHopDbContext> options) : base(options)
    {
    }

    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Edge> Edges => Set<Edge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map Node Entity
        modelBuilder.Entity<Node>(entity =>
        {
            entity.ToTable("Nodes");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Name).IsRequired().HasMaxLength(255);
            entity.Property(n => n.Type).HasConversion<string>(); // Store enum as string
            entity.Property(n => n.PopularityScore).IsRequired();
            entity.HasIndex(n => n.SpotifyId).IsUnique();
        });

        // Map Edge Entity (The Graph Adjacency List)
        modelBuilder.Entity<Edge>(entity =>
        {
            entity.ToTable("Edges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>(); // Store enum as string
            
            // Set up the self-referencing relationship for the graph
            entity.HasOne<Node>()
                  .WithMany()
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Node>()
                  .WithMany()
                  .HasForeignKey(e => e.TargetId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index source and target lookups for ultra-fast BFS traversal
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.TargetId);
        });
    }
}