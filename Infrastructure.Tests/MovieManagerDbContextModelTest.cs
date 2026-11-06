using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

/// <summary>
/// Checks the EF Core model configured in OnModelCreating. It reads the design-time model
/// because index filters and check constraints are not kept in the runtime model.
/// </summary>
[TestClass]
public class MovieManagerDbContextModelTest
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IEntityType _movie = null!;

    [TestInitialize]
    public void SetUp()
    {
        _provider = InMemoryInfrastructure.BuildProvider(Guid.NewGuid().ToString());
        _scope = _provider.CreateScope();

        Type contextType = typeof(DependencyInjection).Assembly.GetTypes()
            .Single(type => type.IsSubclassOf(typeof(DbContext)));
        var context = (DbContext)_scope.ServiceProvider.GetRequiredService(contextType);

        _movie = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Movie))!;
    }

    [TestCleanup]
    public void TearDown()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [TestMethod]
    public void ImdbId_Configured_IsOptionalWithMaxLength20()
    {
        IProperty imdbId = _movie.FindProperty(nameof(Movie.ImdbId))!;

        Assert.IsTrue(imdbId.IsNullable);
        Assert.AreEqual(20, imdbId.GetMaxLength());
    }

    [TestMethod]
    public void ImdbId_Configured_HasUniqueIndexIgnoringNulls()
    {
        IIndex index = _movie.GetIndexes()
            .Single(candidate => candidate.Properties.Select(p => p.Name).SequenceEqual([nameof(Movie.ImdbId)]));

        Assert.IsTrue(index.IsUnique);
        Assert.AreEqual("UX_Movies_ImdbId", index.GetDatabaseName());
        Assert.AreEqual("[ImdbId] IS NOT NULL", index.GetFilter());
    }

    [TestMethod]
    public void ImdbRating_Configured_IsOptionalWithPrecision3Scale1()
    {
        IProperty imdbRating = _movie.FindProperty(nameof(Movie.ImdbRating))!;

        Assert.IsTrue(imdbRating.IsNullable);
        Assert.AreEqual(3, imdbRating.GetPrecision());
        Assert.AreEqual(1, imdbRating.GetScale());
    }

    [TestMethod]
    public void ImdbRating_Configured_HasRangeCheckConstraint()
    {
        ICheckConstraint constraint = _movie.GetCheckConstraints()
            .Single(candidate => candidate.ModelName == "CK_Movies_ImdbRating");

        Assert.AreEqual("[ImdbRating] >= 0 AND [ImdbRating] <= 10", constraint.Sql);
    }
}
