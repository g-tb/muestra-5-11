using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests;

[TestClass]
public class MovieServiceTest
{
    private IMovieService _movieService = null!;

    [TestInitialize]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<Movie>, FakeMovieRepository>();
        services.AddApplication();

        _movieService = services.BuildServiceProvider().GetRequiredService<IMovieService>();
    }

    [TestMethod]
    public void AddOneMovieOkTest()
    {
        MovieDTO movie = new MovieDTO()
        {
            Title = "Avatar",
            Director = "One Director",
            ReleaseYear = 2020
        };

        MovieDTO returnMovie = _movieService.AddMovie(movie);

        Assert.AreEqual(1, returnMovie.Id);
        Assert.AreEqual(movie.Title, returnMovie.Title);
        Assert.AreEqual(movie.Director, returnMovie.Director);
        Assert.AreEqual(movie.ReleaseYear, returnMovie.ReleaseYear);
    }

    [TestMethod]
    public void AddTwoMovieOkTest()
    {
        MovieDTO movieOne = new MovieDTO()
        {
            Title = "Avatar",
            Director = "One Director",
            ReleaseYear = 2020
        };
        MovieDTO movieTwo = new MovieDTO()
        {
            Title = "Avatar 2",
            Director = "One Director",
            ReleaseYear = 2020
        };

        MovieDTO returnMovieOne = _movieService.AddMovie(movieOne);
        MovieDTO returnMovieTwo = _movieService.AddMovie(movieTwo);

        Assert.AreEqual(1, returnMovieOne.Id);
        Assert.AreEqual(2, returnMovieTwo.Id);
    }

    [TestMethod]
    public void ListAllMovieOkTest()
    {
        MovieDTO movieOne = new MovieDTO()
        {
            Title = "Avatar",
            Director = "One Director",
            ReleaseYear = 2020
        };
        MovieDTO movieTwo = new MovieDTO()
        {
            Title = "Avatar 2",
            Director = "One Director",
            ReleaseYear = 2020
        };

        _movieService.AddMovie(movieOne);
        _movieService.AddMovie(movieTwo);

        IList<MovieDTO> resultMovies = _movieService.GetAll();

        Assert.AreEqual("Avatar", resultMovies.FirstOrDefault(x => x.Id == 1)!.Title);
        Assert.AreEqual("Avatar 2", resultMovies.FirstOrDefault(x => x.Id == 2)!.Title);
    }

    [TestMethod]
    public void AddOneMovieTitleEmptyTest()
    {
        AssertThrows<DomainException>(() => _ = new Movie()
        {
            Title = "",
            Director = "One Director",
            ReleaseYear = 2020,
            Budget = 25000
        });
    }

    [TestMethod]
    public void AddOneMovieTitleNullTest()
    {
        AssertThrows<DomainException>(() => _ = new Movie()
        {
            Title = null,
            Director = "One Director",
            ReleaseYear = 2020,
            Budget = 300000
        });
    }

    [TestMethod]
    public void AddOneMovieBudgetNullTest()
    {
        AssertThrows<DomainException>(() => _ = new Movie()
        {
            Title = "Avatar",
            Director = "One Director",
            ReleaseYear = 2020,
            Budget = -500000
        });
    }

    [TestMethod]
    public void AddMovieSameNameOkTest()
    {
        MovieDTO movieOne = new MovieDTO()
        {
            Title = "Avatar",
            Director = "One Director",
            ReleaseYear = 2020
        };
        MovieDTO movieTwo = new MovieDTO()
        {
            Title = "Avatar",
            Director = "One Director",
            ReleaseYear = 2020
        };

        _movieService.AddMovie(movieOne);

        var exception = AssertThrows<LogicException>(() => _movieService.AddMovie(movieTwo));
        Assert.AreEqual("Can't add movie with same Title", exception.Message);
    }

    private static TException AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).FullName}.");
        throw new InvalidOperationException("Assert.Fail did not throw.");
    }

    private class FakeMovieRepository : IRepository<Movie>
    {
        private readonly List<Movie> _movies = new();

        public Movie Add(Movie oneElement)
        {
            oneElement.Id = _movies.OrderByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefault() + 1;
            _movies.Add(oneElement);
            return oneElement;
        }

        public Movie? Find(Func<Movie, bool> filter)
        {
            return _movies.Where(filter).FirstOrDefault();
        }

        public IList<Movie> FindAll()
        {
            return _movies;
        }

        public Movie? Update(Movie updatedEntity)
        {
            Movie? foundMovie = Find(x => x.Id == updatedEntity.Id);
            foundMovie?.Update(updatedEntity);
            return foundMovie;
        }

        public void Delete(int id)
        {
            _movies.RemoveAll(x => x.Id == id);
        }
    }
}
