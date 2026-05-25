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

        // --- Configure Node Schema ---
        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Name).IsRequired().HasMaxLength(255);
            // Index the name for lightning-fast autocomplete search queries later
            entity.HasIndex(n => n.Name); 
        });

        // --- Configure Edge (Graph Relationship) Schema ---
        modelBuilder.Entity<Edge>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Map the foreign key for the source artist
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Map the foreign key for the target artist
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.TargetId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create a composite index to keep graph edge lookup lookups lightning fast
            entity.HasIndex(e => new { e.SourceId, e.TargetId });
        });
    }
}