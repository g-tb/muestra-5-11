using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Tests;

[TestClass]
public class MovieSqlRepositoryTest : InMemoryRepositoryTestBase<Movie>
{
    private Category _sciFi = null!;
    private Category _drama = null!;
    private Actor _keanu = null!;
    private Actor _carrie = null!;
    private Actor _laurence = null!;

    [TestInitialize]
    public void SetUpRelations()
    {
        IRepository<Category> categories = RepositoryInNewScope<Category>();
        IRepository<Actor> actors = RepositoryInNewScope<Actor>();
        _sciFi = categories.Add(new Category { Name = "Science Fiction" });
        _drama = categories.Add(new Category { Name = "Drama" });
        _keanu = actors.Add(new Actor { Name = "Keanu Reeves" });
        _carrie = actors.Add(new Actor { Name = "Carrie-Anne Moss" });
        _laurence = actors.Add(new Actor { Name = "Laurence Fishburne" });
    }

    [TestMethod]
    public void Add_MovieWithoutRelations_AssignsIdAndPersists()
    {
        Movie added = Repository.Add(new Movie { Title = "Avatar", Director = "James Cameron", ReleaseYear = 2009 });

        Assert.AreNotEqual(0, added.Id);
        Movie? stored = FreshRepository().Find(movie => movie.Id == added.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("Avatar", stored.Title);
        Assert.AreEqual("James Cameron", stored.Director);
        Assert.AreEqual(2009, stored.ReleaseYear);
        Assert.IsNull(stored.Category);
        Assert.AreEqual(0, stored.Actors.Count);
    }

    [TestMethod]
    public void Add_WithSavedCategoryAndActors_PersistsRelations()
    {
        Movie added = Repository.Add(NewMatrix(Detached(_sciFi), Detached(_keanu), Detached(_carrie)));

        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        Assert.AreEqual(_sciFi.Id, stored.Category!.Id);
        Assert.AreEqual("Science Fiction", stored.Category.Name);
        CollectionAssert.AreEquivalent(new[] { _keanu.Id, _carrie.Id }, ActorIds(stored));
    }

    [TestMethod]
    public void Add_WithUnsavedCategory_DropsCategory()
    {
        Movie added = Repository.Add(NewMatrix(new Category { Name = "Never saved" }));

        Assert.IsNull(FreshRepository().Find(movie => movie.Id == added.Id)!.Category);
        Assert.AreEqual(2, RepositoryInNewScope<Category>().FindAll().Count);
    }

    [TestMethod]
    public void Add_WithUnknownCategoryId_DropsCategory()
    {
        Movie added = Repository.Add(NewMatrix(new Category { Id = 999, Name = "Missing" }));

        Assert.IsNull(FreshRepository().Find(movie => movie.Id == added.Id)!.Category);
    }

    [TestMethod]
    public void Add_WithUnknownActorIds_DropsThoseActors()
    {
        Movie added = Repository.Add(NewMatrix(null, Detached(_keanu), new Actor { Id = 999, Name = "Missing" }));

        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        CollectionAssert.AreEquivalent(new[] { _keanu.Id }, ActorIds(stored));
        Assert.AreEqual(3, RepositoryInNewScope<Actor>().FindAll().Count);
    }

    [TestMethod]
    public void Add_WithDuplicateActors_StoresEachActorOnce()
    {
        Movie added = Repository.Add(NewMatrix(null, Detached(_keanu), Detached(_keanu)));

        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        CollectionAssert.AreEquivalent(new[] { _keanu.Id }, ActorIds(stored));
    }

    [TestMethod]
    public void Find_ExistingMatch_ReturnsMovieWithRelations()
    {
        Repository.Add(new Movie { Title = "Avatar" });
        Movie matrix = Repository.Add(NewMatrix(Detached(_sciFi), Detached(_keanu)));

        Movie? found = FreshRepository().Find(movie => movie.Title == "The Matrix");

        Assert.IsNotNull(found);
        Assert.AreEqual(matrix.Id, found.Id);
        Assert.AreEqual(_sciFi.Id, found.Category!.Id);
        CollectionAssert.AreEquivalent(new[] { _keanu.Id }, ActorIds(found));
    }

    [TestMethod]
    public void Find_MissingId_ReturnsNull()
    {
        Repository.Add(new Movie { Title = "Avatar" });

        Movie? found = Repository.Find(movie => movie.Id == 999);

        Assert.IsNull(found);
    }

    [TestMethod]
    public void FindAll_EmptyStore_ReturnsEmptyList()
    {
        IList<Movie> all = Repository.FindAll();

        Assert.AreEqual(0, all.Count);
    }

    [TestMethod]
    public void FindAll_SeveralMovies_ReturnsAllWithRelations()
    {
        Repository.Add(new Movie { Title = "Avatar" });
        Repository.Add(NewMatrix(Detached(_sciFi), Detached(_keanu), Detached(_carrie)));

        IList<Movie> all = FreshRepository().FindAll();

        Assert.AreEqual(2, all.Count);
        Movie matrix = all.Single(movie => movie.Title == "The Matrix");
        Assert.AreEqual(_sciFi.Id, matrix.Category!.Id);
        CollectionAssert.AreEquivalent(new[] { _keanu.Id, _carrie.Id }, ActorIds(matrix));
        Assert.IsNull(all.Single(movie => movie.Title == "Avatar").Category);
    }

    [TestMethod]
    public void FindAll_ReturnedListModified_DoesNotAffectRepository()
    {
        Repository.Add(new Movie { Title = "Avatar" });

        IList<Movie> all = Repository.FindAll();
        all.Clear();

        Assert.AreEqual(1, Repository.FindAll().Count);
    }

    [TestMethod]
    public void Update_DetachedMovieWithNewCategoryAndActors_ReplacesFieldsAndRelations()
    {
        Movie added = Repository.Add(NewMatrix(Detached(_sciFi), Detached(_keanu), Detached(_carrie)));

        Movie? updated = FreshRepository().Update(new Movie
        {
            Id = added.Id,
            Title = "The Matrix Reloaded",
            Director = "The Wachowskis",
            ReleaseYear = 2003,
            Category = Detached(_drama),
            Actors = [Detached(_laurence)]
        });

        Assert.IsNotNull(updated);
        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        Assert.AreEqual("The Matrix Reloaded", stored.Title);
        Assert.AreEqual("The Wachowskis", stored.Director);
        Assert.AreEqual(2003, stored.ReleaseYear);
        Assert.AreEqual(_drama.Id, stored.Category!.Id);
        CollectionAssert.AreEquivalent(new[] { _laurence.Id }, ActorIds(stored));
    }

    [TestMethod]
    public void Update_DetachedMovieWithoutCategoryAndActors_ClearsRelations()
    {
        Movie added = Repository.Add(NewMatrix(Detached(_sciFi), Detached(_keanu)));

        FreshRepository().Update(new Movie { Id = added.Id, Title = "The Matrix" });

        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        Assert.IsNull(stored.Category);
        Assert.AreEqual(0, stored.Actors.Count);
        Assert.AreEqual(3, RepositoryInNewScope<Actor>().FindAll().Count);
        Assert.AreEqual(2, RepositoryInNewScope<Category>().FindAll().Count);
    }

    [TestMethod]
    public void Update_DetachedMovieWithUnknownActorIds_DropsThoseActors()
    {
        Movie added = Repository.Add(NewMatrix(null, Detached(_keanu)));

        FreshRepository().Update(new Movie
        {
            Id = added.Id,
            Title = "The Matrix",
            Actors = [Detached(_carrie), new Actor { Id = 999, Name = "Missing" }, Detached(_carrie)]
        });

        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        CollectionAssert.AreEquivalent(new[] { _carrie.Id }, ActorIds(stored));
    }

    [TestMethod]
    public void Update_MovieAlreadyLoadedInSameScope_PersistsScalarFieldsAndCategory()
    {
        Movie added = Repository.Add(NewMatrix(Detached(_sciFi), Detached(_keanu)));
        Movie loaded = Repository.Find(movie => movie.Id == added.Id)!;

        loaded.Title = "The Matrix Reloaded";
        loaded.Category = Detached(_drama);
        Repository.Update(loaded);

        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        Assert.AreEqual("The Matrix Reloaded", stored.Title);
        Assert.AreEqual(_drama.Id, stored.Category!.Id);
    }

    [TestMethod]
    public void Update_MovieAlreadyLoadedInSameScope_KeepsChangedActors()
    {
        Movie added = Repository.Add(NewMatrix(null, Detached(_keanu)));
        Movie loaded = Repository.Find(movie => movie.Id == added.Id)!;

        loaded.Actors = [Detached(_carrie), Detached(_laurence)];
        Movie? updated = Repository.Update(loaded);

        Assert.IsNotNull(updated);
        CollectionAssert.AreEquivalent(new[] { _carrie.Id, _laurence.Id }, ActorIds(updated));
        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        CollectionAssert.AreEquivalent(new[] { _carrie.Id, _laurence.Id }, ActorIds(stored));
    }

    [TestMethod]
    public void Update_MovieAlreadyLoadedInSameScopeWithUnchangedActors_KeepsActors()
    {
        Movie added = Repository.Add(NewMatrix(null, Detached(_keanu), Detached(_carrie)));
        Movie loaded = Repository.Find(movie => movie.Id == added.Id)!;

        loaded.Title = "The Matrix Reloaded";
        Repository.Update(loaded);

        Movie stored = FreshRepository().Find(movie => movie.Id == added.Id)!;
        CollectionAssert.AreEquivalent(new[] { _keanu.Id, _carrie.Id }, ActorIds(stored));
    }

    [TestMethod]
    public void Update_MissingId_ReturnsNull()
    {
        Repository.Add(new Movie { Title = "Avatar" });

        Movie? updated = Repository.Update(new Movie { Id = 999, Title = "Ghost" });

        Assert.IsNull(updated);
        IList<Movie> all = FreshRepository().FindAll();
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual("Avatar", all[0].Title);
    }

    [TestMethod]
    public void Delete_ExistingId_RemovesMovieButKeepsActorsAndCategory()
    {
        Movie matrix = Repository.Add(NewMatrix(Detached(_sciFi), Detached(_keanu)));
        Repository.Add(new Movie { Title = "Avatar" });

        Repository.Delete(matrix.Id);

        IRepository<Movie> fresh = FreshRepository();
        Assert.IsNull(fresh.Find(movie => movie.Id == matrix.Id));
        Assert.AreEqual(1, fresh.FindAll().Count);
        Assert.AreEqual(3, RepositoryInNewScope<Actor>().FindAll().Count);
        Assert.AreEqual(2, RepositoryInNewScope<Category>().FindAll().Count);
    }

    [TestMethod]
    public void Delete_MissingId_DoesNothing()
    {
        Repository.Add(new Movie { Title = "Avatar" });

        Repository.Delete(999);

        Assert.AreEqual(1, FreshRepository().FindAll().Count);
    }

    private static Movie NewMatrix(Category? category, params Actor[] actors)
    {
        return new Movie
        {
            Title = "The Matrix",
            Director = "The Wachowskis",
            ReleaseYear = 1999,
            Category = category,
            Actors = actors.ToList()
        };
    }

    // New instances carrying only the Id, as the UI/DTO layer would send them.
    private static Category Detached(Category category) => new() { Id = category.Id, Name = category.Name };

    private static Actor Detached(Actor actor) => new() { Id = actor.Id, Name = actor.Name };

    private static List<int> ActorIds(Movie movie) => movie.Actors.Select(actor => actor.Id).ToList();
}
