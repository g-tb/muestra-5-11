using Domain.Entities;
using Domain.Exceptions;

namespace Application.Tests;

[TestClass]
public class MovieTest
{
    private Movie _movie = null!;

    [TestInitialize]
    public void SetUp()
    {
        _movie = new Movie { Title = "The Matrix" };
    }

    [TestMethod]
    public void ImdbRating_BelowZero_ThrowsDomainException()
    {
        var exception = AssertThrows<DomainException>(() => _movie.ImdbRating = -0.1m);

        Assert.AreEqual("IMDb rating must be between 0 and 10", exception.Message);
    }

    [TestMethod]
    public void ImdbRating_AboveTen_ThrowsDomainException()
    {
        var exception = AssertThrows<DomainException>(() => _movie.ImdbRating = 10.1m);

        Assert.AreEqual("IMDb rating must be between 0 and 10", exception.Message);
    }

    [TestMethod]
    public void ImdbRating_ZeroAndTen_AreAccepted()
    {
        _movie.ImdbRating = 0m;
        Assert.AreEqual(0m, _movie.ImdbRating);

        _movie.ImdbRating = 10m;
        Assert.AreEqual(10m, _movie.ImdbRating);
    }

    [TestMethod]
    public void ImdbRating_Null_IsAccepted()
    {
        _movie.ImdbRating = 8.7m;

        _movie.ImdbRating = null;

        Assert.IsNull(_movie.ImdbRating);
    }

    [TestMethod]
    public void ImdbId_Empty_ThrowsDomainException()
    {
        var exception = AssertThrows<DomainException>(() => _movie.ImdbId = "");

        Assert.AreEqual("IMDb id must not be empty", exception.Message);
    }

    [TestMethod]
    public void ImdbId_Whitespace_ThrowsDomainException()
    {
        var exception = AssertThrows<DomainException>(() => _movie.ImdbId = "   ");

        Assert.AreEqual("IMDb id must not be empty", exception.Message);
    }

    [TestMethod]
    public void ImdbId_Null_IsAccepted()
    {
        _movie.ImdbId = "tt0133093";

        _movie.ImdbId = null;

        Assert.IsNull(_movie.ImdbId);
    }

    [TestMethod]
    public void ImdbId_WithSpacesAndUppercase_IsTrimmedAndLowercased()
    {
        _movie.ImdbId = "  TT0133093 ";

        Assert.AreEqual("tt0133093", _movie.ImdbId);
    }

    [TestMethod]
    public void Update_WithImdbFields_CopiesThem()
    {
        var updated = new Movie { Title = "The Matrix", ImdbId = "tt0133093", ImdbRating = 8.7m };

        _movie.Update(updated);

        Assert.AreEqual("tt0133093", _movie.ImdbId);
        Assert.AreEqual(8.7m, _movie.ImdbRating);
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
}
