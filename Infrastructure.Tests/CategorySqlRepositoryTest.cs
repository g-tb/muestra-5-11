using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Tests;

[TestClass]
public class CategorySqlRepositoryTest : InMemoryRepositoryTestBase<Category>
{
    [TestMethod]
    public void Add_NewCategory_AssignsIdAndPersists()
    {
        Category added = Repository.Add(new Category { Name = "Drama" });

        Assert.AreNotEqual(0, added.Id);
        Category? stored = FreshRepository().Find(category => category.Id == added.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("Drama", stored.Name);
    }

    [TestMethod]
    public void Find_ExistingMatch_ReturnsCategory()
    {
        Repository.Add(new Category { Name = "Drama" });
        Category comedy = Repository.Add(new Category { Name = "Comedy" });

        Category? found = FreshRepository().Find(category => category.Name == "Comedy");

        Assert.IsNotNull(found);
        Assert.AreEqual(comedy.Id, found.Id);
    }

    [TestMethod]
    public void Find_MissingId_ReturnsNull()
    {
        Repository.Add(new Category { Name = "Drama" });

        Category? found = Repository.Find(category => category.Id == 999);

        Assert.IsNull(found);
    }

    [TestMethod]
    public void FindAll_EmptyStore_ReturnsEmptyList()
    {
        IList<Category> all = Repository.FindAll();

        Assert.AreEqual(0, all.Count);
    }

    [TestMethod]
    public void FindAll_SeveralCategories_ReturnsAll()
    {
        Repository.Add(new Category { Name = "Drama" });
        Repository.Add(new Category { Name = "Comedy" });

        IList<Category> all = FreshRepository().FindAll();

        Assert.AreEqual(2, all.Count);
        CollectionAssert.AreEquivalent(new[] { "Drama", "Comedy" }, all.Select(category => category.Name).ToList());
    }

    [TestMethod]
    public void FindAll_ReturnedListModified_DoesNotAffectRepository()
    {
        Repository.Add(new Category { Name = "Drama" });

        IList<Category> all = Repository.FindAll();
        all.Clear();

        Assert.AreEqual(1, Repository.FindAll().Count);
    }

    [TestMethod]
    public void Update_DetachedCategoryWithExistingId_PersistsChanges()
    {
        Category added = Repository.Add(new Category { Name = "Drama" });

        Category? updated = FreshRepository().Update(new Category { Id = added.Id, Name = "Thriller" });

        Assert.IsNotNull(updated);
        Assert.AreEqual(added.Id, updated.Id);
        Assert.AreEqual("Thriller", updated.Name);
        Assert.AreEqual("Thriller", FreshRepository().Find(category => category.Id == added.Id)!.Name);
    }

    [TestMethod]
    public void Update_CategoryAlreadyLoaded_PersistsChanges()
    {
        Category added = Repository.Add(new Category { Name = "Drama" });
        Category loaded = Repository.Find(category => category.Id == added.Id)!;

        loaded.Name = "Thriller";
        Category? updated = Repository.Update(loaded);

        Assert.IsNotNull(updated);
        Assert.AreEqual("Thriller", FreshRepository().Find(category => category.Id == added.Id)!.Name);
    }

    [TestMethod]
    public void Update_MissingId_ReturnsNull()
    {
        Repository.Add(new Category { Name = "Drama" });

        Category? updated = Repository.Update(new Category { Id = 999, Name = "Thriller" });

        Assert.IsNull(updated);
        IList<Category> all = FreshRepository().FindAll();
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual("Drama", all[0].Name);
    }

    [TestMethod]
    public void Delete_ExistingId_RemovesCategory()
    {
        Category drama = Repository.Add(new Category { Name = "Drama" });
        Repository.Add(new Category { Name = "Comedy" });

        Repository.Delete(drama.Id);

        IRepository<Category> fresh = FreshRepository();
        Assert.IsNull(fresh.Find(category => category.Id == drama.Id));
        Assert.AreEqual(1, fresh.FindAll().Count);
    }

    [TestMethod]
    public void Delete_MissingId_DoesNothing()
    {
        Repository.Add(new Category { Name = "Drama" });

        Repository.Delete(999);

        Assert.AreEqual(1, FreshRepository().FindAll().Count);
    }
}
