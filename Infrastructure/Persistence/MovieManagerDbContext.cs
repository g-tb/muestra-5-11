using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal class MovieManagerDbContext(DbContextOptions<MovieManagerDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", "dbo");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).ValueGeneratedOnAdd();
            entity.Property(category => category.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Actor>(entity =>
        {
            entity.ToTable("Actors", "dbo");
            entity.HasKey(actor => actor.Id);
            entity.Property(actor => actor.Id).ValueGeneratedOnAdd();
            entity.Property(actor => actor.Name).HasMaxLength(200);
            entity.Property(actor => actor.Bio);
            entity.Property(actor => actor.BirthDate).HasColumnType("datetime2(7)");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("Movies", "dbo");
            entity.HasKey(movie => movie.Id);
            entity.Property(movie => movie.Id).ValueGeneratedOnAdd();
            entity.Property(movie => movie.Title).IsRequired().HasMaxLength(200);
            entity.Property(movie => movie.Director).HasMaxLength(200);
            entity.Property(movie => movie.ReleaseYear);
            entity.Ignore(movie => movie.Budget);
            entity.HasIndex(movie => movie.Title).IsUnique().HasDatabaseName("UX_Movies_Title");

            entity.HasOne(movie => movie.Category)
                .WithMany()
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Movies_Categories_CategoryId");

            entity.HasMany(movie => movie.Actors)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "MovieActor",
                    right => right.HasOne<Actor>()
                        .WithMany()
                        .HasForeignKey("ActorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("FK_MovieActors_Actors_ActorId"),
                    left => left.HasOne<Movie>()
                        .WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("FK_MovieActors_Movies_MovieId"),
                    join =>
                    {
                        join.ToTable("MovieActors", "dbo");
                        join.HasKey("MovieId", "ActorId");
                        join.HasIndex("ActorId").HasDatabaseName("IX_MovieActors_ActorId");
                    });
        });
    }
}
