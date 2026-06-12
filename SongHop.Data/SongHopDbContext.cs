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

            entity.HasOne<Node>()
                  .WithMany()
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Node>()
                  .WithMany()
                  .HasForeignKey(e => e.TargetId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            //  Allows multiple distinct relationships between the same two artists
            entity.HasIndex(e => new { e.SourceId, e.TargetId, e.Type, e.ContextTitle })
                  .IsUnique();
        });
    }
}