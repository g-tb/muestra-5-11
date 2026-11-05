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
        movie.Category = movie.Category?.Id > 0
            ? DbContext.Categories.Find(movie.Category.Id)
            : null;

        movie.Actors = movie.Actors
            .Select(actor => DbContext.Actors.Find(actor.Id))
            .OfType<Actor>()
            .DistinctBy(actor => actor.Id)
            .ToList();
    }

    protected override void ApplyChanges(Movie target, Movie source)
    {
        target.Title = source.Title;
        target.Director = source.Director;
        target.ReleaseYear = source.ReleaseYear;
        target.Category = source.Category?.Id > 0
            ? DbContext.Categories.Find(source.Category.Id)
            : null;

        target.Actors.Clear();
        foreach (int actorId in source.Actors.Select(actor => actor.Id).Where(id => id > 0).Distinct())
        {
            Actor? actor = DbContext.Actors.Find(actorId);
            if (actor is not null)
            {
                target.Actors.Add(actor);
            }
        }
    }
}
