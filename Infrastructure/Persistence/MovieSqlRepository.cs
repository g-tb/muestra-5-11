using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal class MovieSqlRepository(MovieManagerDbContext dbContext) : IRepository<Movie>
{
    public Movie Add(Movie oneElement)
    {
        AttachMovieRelationships(oneElement);
        dbContext.Movies.Add(oneElement);
        dbContext.SaveChanges();
        return oneElement;
    }

    public Movie? Find(Func<Movie, bool> filter)
    {
        return LoadMovies().AsEnumerable().FirstOrDefault(filter);
    }

    public IList<Movie> FindAll()
    {
        return LoadMovies().ToList();
    }

    public Movie? Update(Movie updatedEntity)
    {
        Movie? foundMovie = LoadMovies().FirstOrDefault(movie => movie.Id == updatedEntity.Id);
        if (foundMovie is null)
        {
            return null;
        }

        foundMovie.Title = updatedEntity.Title;
        foundMovie.Director = updatedEntity.Director;
        foundMovie.ReleaseYear = updatedEntity.ReleaseYear;
        foundMovie.Category = updatedEntity.Category?.Id > 0
            ? dbContext.Categories.Find(updatedEntity.Category.Id)
            : null;

        foundMovie.Actors.Clear();
        foreach (int actorId in updatedEntity.Actors.Select(actor => actor.Id).Where(id => id > 0).Distinct())
        {
            Actor? actor = dbContext.Actors.Find(actorId);
            if (actor is not null)
            {
                foundMovie.Actors.Add(actor);
            }
        }

        dbContext.SaveChanges();
        return foundMovie;
    }

    public void Delete(int id)
    {
        Movie? foundMovie = LoadMovies().FirstOrDefault(movie => movie.Id == id);
        if (foundMovie is null)
        {
            return;
        }

        dbContext.Movies.Remove(foundMovie);
        dbContext.SaveChanges();
    }

    private IQueryable<Movie> LoadMovies()
    {
        return dbContext.Movies
            .Include(movie => movie.Category)
            .Include(movie => movie.Actors)
            .AsSplitQuery();
    }

    private void AttachMovieRelationships(Movie movie)
    {
        movie.Category = movie.Category?.Id > 0
            ? dbContext.Categories.Find(movie.Category.Id)
            : null;

        movie.Actors = movie.Actors
            .Select(actor => dbContext.Actors.Find(actor.Id))
            .OfType<Actor>()
            .DistinctBy(actor => actor.Id)
            .ToList();
    }
}
