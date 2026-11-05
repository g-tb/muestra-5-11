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
        Movie newMovie = moviesRepository.Add(movie.toEntity());
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

    public MovieDTO FindById(int id)
    {
        return MovieDTO.fromEntity(moviesRepository.Find(x => x.Id == id)!);
    }

    public MovieDTO Update(MovieDTO updatedMovie)
    {
        return MovieDTO.fromEntity(moviesRepository.Update(updatedMovie.toEntity())!);
    }

    public void Delete(int id)
    {
        moviesRepository.Delete(id);
    }
}
