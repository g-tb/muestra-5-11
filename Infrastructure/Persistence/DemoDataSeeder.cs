using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal class DemoDataSeeder(MovieManagerDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Category action = await GetOrCreateCategoryAsync("Action", cancellationToken);
        Category drama = await GetOrCreateCategoryAsync("Drama", cancellationToken);
        Category scienceFiction = await GetOrCreateCategoryAsync("Science Fiction", cancellationToken);

        Actor keanuReeves = await GetOrCreateActorAsync(
            "Keanu Reeves",
            "Canadian actor known for action and science fiction roles.",
            new DateTime(1964, 9, 2),
            cancellationToken);
        Actor carrieAnneMoss = await GetOrCreateActorAsync(
            "Carrie-Anne Moss",
            "Canadian actor known for The Matrix films.",
            new DateTime(1967, 8, 21),
            cancellationToken);
        Actor leonardoDiCaprio = await GetOrCreateActorAsync(
            "Leonardo DiCaprio",
            "American actor and film producer.",
            new DateTime(1974, 11, 11),
            cancellationToken);
        Actor elliotPage = await GetOrCreateActorAsync(
            "Elliot Page",
            "Canadian actor and producer.",
            new DateTime(1987, 2, 21),
            cancellationToken);
        Actor timRobbins = await GetOrCreateActorAsync(
            "Tim Robbins",
            "American actor and filmmaker.",
            new DateTime(1958, 10, 16),
            cancellationToken);

        await UpsertMovieAsync(
            "The Matrix",
            "Lana Wachowski, Lilly Wachowski",
            1999,
            scienceFiction,
            [keanuReeves, carrieAnneMoss],
            cancellationToken);
        await UpsertMovieAsync(
            "Inception",
            "Christopher Nolan",
            2010,
            scienceFiction,
            [leonardoDiCaprio, elliotPage],
            cancellationToken);
        await UpsertMovieAsync(
            "John Wick",
            "Chad Stahelski",
            2014,
            action,
            [keanuReeves],
            cancellationToken);
        await UpsertMovieAsync(
            "The Shawshank Redemption",
            "Frank Darabont",
            1994,
            drama,
            [timRobbins],
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetOrCreateCategoryAsync(string name, CancellationToken cancellationToken)
    {
        Category? category = dbContext.Categories.Local.FirstOrDefault(candidate => candidate.Name == name)
                             ?? await dbContext.Categories.FirstOrDefaultAsync(
                                 candidate => candidate.Name == name,
                                 cancellationToken);

        if (category is not null)
        {
            return category;
        }

        category = new Category { Name = name };
        dbContext.Categories.Add(category);
        return category;
    }

    private async Task<Actor> GetOrCreateActorAsync(
        string name,
        string bio,
        DateTime birthDate,
        CancellationToken cancellationToken)
    {
        Actor? actor = dbContext.Actors.Local.FirstOrDefault(candidate => candidate.Name == name)
                       ?? await dbContext.Actors.FirstOrDefaultAsync(
                           candidate => candidate.Name == name,
                           cancellationToken);

        if (actor is null)
        {
            actor = new Actor { Name = name };
            dbContext.Actors.Add(actor);
        }

        actor.Bio = bio;
        actor.BirthDate = birthDate;
        return actor;
    }

    private async Task UpsertMovieAsync(
        string title,
        string director,
        int releaseYear,
        Category category,
        IReadOnlyCollection<Actor> actors,
        CancellationToken cancellationToken)
    {
        Movie? movie = dbContext.Movies.Local.FirstOrDefault(candidate => candidate.Title == title)
                       ?? await dbContext.Movies
                           .Include(candidate => candidate.Actors)
                           .Include(candidate => candidate.Category)
                           .FirstOrDefaultAsync(candidate => candidate.Title == title, cancellationToken);

        if (movie is null)
        {
            movie = new Movie { Title = title };
            dbContext.Movies.Add(movie);
        }

        movie.Director = director;
        movie.ReleaseYear = releaseYear;
        movie.Category = category;
        movie.Actors.Clear();

        foreach (Actor actor in actors)
        {
            movie.Actors.Add(actor);
        }
    }
}
