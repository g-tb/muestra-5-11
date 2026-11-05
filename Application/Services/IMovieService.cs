// ReSharper disable UnusedMethodReturnValue.Global

using Application.Dtos;

namespace Application.Services;

public interface IMovieService
{
    MovieDTO AddMovie(MovieDTO movie);
    IList<MovieDTO> GetAll();
    MovieDTO FindById(int id);
    MovieDTO Update(MovieDTO updatedMovie);
    void Delete(int id);
}
