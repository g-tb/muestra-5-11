using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal class MovieSqlRepository(MovieManagerDbContext dbContext) : SqlRepository<Movie>(dbContext)
{
    protected override IQueryable<Movie> Query()
    {
        return Set
            .Include(movie => movie.Category)
            .Include(movie => movie.Actors)
            .AsSplitQuery();
    }

    protected override Movie? FindTracked(int id)
    {
        return Query().FirstOrDefault(movie => movie.Id == id);
    }

    protected override void PrepareForAdd(Movie movie)
    {
        movie.Category = ResolveCategory(movie.Category);
        movie.Actors = ResolveActors(movie.Actors);
    }

    protected override void ApplyChanges(Movie target, Movie source)
    {
        target.Title = source.Title;
        target.Director = source.Director;
        target.ReleaseYear = source.ReleaseYear;
        target.ImdbId = source.ImdbId;
        target.ImdbRating = source.ImdbRating;
        target.Category = ResolveCategory(source.Category);

        // Resolve before clearing: source and target may be the same tracked instance.
        List<Actor> actors = ResolveActors(source.Actors);
        target.Actors.Clear();
        target.Actors.AddRange(actors);
    }

    private Category? ResolveCategory(Category? category)
    {
        return category?.Id > 0
            ? DbContext.Categories.Find(category.Id)
            : null;
    }

    private List<Actor> ResolveActors(IEnumerable<Actor> actors)
    {
        return actors
            .Select(actor => actor.Id)
            .Where(id => id > 0)
            .Distinct()
            .Select(id => DbContext.Actors.Find(id))
            .OfType<Actor>()
            .ToList();
    }
}
