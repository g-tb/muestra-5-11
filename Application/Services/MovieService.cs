using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

internal class MovieService(IRepository<Movie> moviesRepository) : IMovieService
{
    public MovieDTO AddMovie(MovieDTO movie)
    {
        ValidateMovieName(movie);
        Movie entity = movie.toEntity();
        ValidateUniqueImdbId(entity);
        Movie newMovie = moviesRepository.Add(entity);
        return MovieDTO.fromEntity(newMovie);
    }

    public IList<MovieDTO> GetAll()
    {
        return moviesRepository.FindAll().Select(MovieDTO.fromEntity).ToList();
    }

    private void ValidateMovieName(MovieDTO movie)
    {
        if (moviesRepository.Find(x => x.Title == movie.Title) != null)
        {
            throw new LogicException("Can't add movie with same Title");
        }
    }

    // Movie stores the IMDb id trimmed and lowercased, so this comparison doesn't depend on the database collation.
    private void ValidateUniqueImdbId(Movie movie)
    {
        if (movie.ImdbId is null)
        {
            return;
        }

        string imdbId = movie.ImdbId;
        int movieId = movie.Id;
        if (moviesRepository.Find(x => x.ImdbId == imdbId && x.Id != movieId) != null)
        {
            throw new LogicException("Can't add movie with same IMDb id");
        }
    }

    public MovieDTO FindById(int id)
    {
        return MovieDTO.fromEntity(moviesRepository.Find(x => x.Id == id)!);
    }

    public MovieDTO Update(MovieDTO updatedMovie)
    {
        Movie entity = updatedMovie.toEntity();
        ValidateUniqueImdbId(entity);
        return MovieDTO.fromEntity(moviesRepository.Update(entity)!);
    }

    public void Delete(int id)
    {
        moviesRepository.Delete(id);
    }
}
