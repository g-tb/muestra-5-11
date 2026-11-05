using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Tests;

[TestClass]
public class ActorSqlRepositoryTest : InMemoryRepositoryTestBase<Actor>
{
    private static readonly DateTime KeanuBirthDate = new(1964, 9, 2);

    [TestMethod]
    public void Add_NewActor_AssignsIdAndPersists()
    {
        Actor added = Repository.Add(NewKeanu());

        Assert.AreNotEqual(0, added.Id);
        Actor? stored = FreshRepository().Find(actor => actor.Id == added.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("Keanu Reeves", stored.Name);
        Assert.AreEqual("Canadian actor", stored.Bio);
        Assert.AreEqual(KeanuBirthDate, stored.BirthDate);
    }

    [TestMethod]
    public void Add_ActorWithoutOptionalFields_PersistsNulls()
    {
        Actor added = Repository.Add(new Actor { Name = "Unknown" });

        Actor stored = FreshRepository().Find(actor => actor.Id == added.Id)!;
        Assert.IsNull(stored.Bio);
        Assert.IsNull(stored.BirthDate);
    }

    [TestMethod]
    public void Find_ExistingMatch_ReturnsActor()
    {
        Repository.Add(NewKeanu());
        Actor carrie = Repository.Add(new Actor { Name = "Carrie-Anne Moss" });

        Actor? found = FreshRepository().Find(actor => actor.Name == "Carrie-Anne Moss");

        Assert.IsNotNull(found);
        Assert.AreEqual(carrie.Id, found.Id);
    }

    [TestMethod]
    public void Find_MissingId_ReturnsNull()
    {
        Repository.Add(NewKeanu());

        Actor? found = Repository.Find(actor => actor.Id == 999);

        Assert.IsNull(found);
    }

    [TestMethod]
    public void FindAll_EmptyStore_ReturnsEmptyList()
    {
        IList<Actor> all = Repository.FindAll();

        Assert.AreEqual(0, all.Count);
    }

    [TestMethod]
    public void FindAll_SeveralActors_ReturnsAll()
    {
        Repository.Add(NewKeanu());
        Repository.Add(new Actor { Name = "Carrie-Anne Moss" });

        IList<Actor> all = FreshRepository().FindAll();

        Assert.AreEqual(2, all.Count);
        CollectionAssert.AreEquivalent(new[] { "Keanu Reeves", "Carrie-Anne Moss" }, all.Select(actor => actor.Name).ToList());
    }

    [TestMethod]
    public void FindAll_ReturnedListModified_DoesNotAffectRepository()
    {
        Repository.Add(NewKeanu());

        IList<Actor> all = Repository.FindAll();
        all.Clear();

        Assert.AreEqual(1, Repository.FindAll().Count);
    }

    [TestMethod]
    public void Update_DetachedActorWithExistingId_PersistsAllFields()
    {
        Actor added = Repository.Add(NewKeanu());
        var newBirthDate = new DateTime(1967, 8, 21);

        Actor? updated = FreshRepository().Update(new Actor
        {
            Id = added.Id,
            Name = "Carrie-Anne Moss",
            Bio = "Updated bio",
            BirthDate = newBirthDate
        });

        Assert.IsNotNull(updated);
        Assert.AreEqual(added.Id, updated.Id);
        Actor stored = FreshRepository().Find(actor => actor.Id == added.Id)!;
        Assert.AreEqual("Carrie-Anne Moss", stored.Name);
        Assert.AreEqual("Updated bio", stored.Bio);
        Assert.AreEqual(newBirthDate, stored.BirthDate);
    }

    [TestMethod]
    public void Update_ActorAlreadyLoaded_PersistsChanges()
    {
        Actor added = Repository.Add(NewKeanu());
        Actor loaded = Repository.Find(actor => actor.Id == added.Id)!;

        loaded.Bio = "Updated bio";
        Actor? updated = Repository.Update(loaded);

        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated bio", FreshRepository().Find(actor => actor.Id == added.Id)!.Bio);
    }

    [TestMethod]
    public void Update_MissingId_ReturnsNull()
    {
        Repository.Add(NewKeanu());

        Actor? updated = Repository.Update(new Actor { Id = 999, Name = "Nobody" });

        Assert.IsNull(updated);
        IList<Actor> all = FreshRepository().FindAll();
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual("Keanu Reeves", all[0].Name);
    }

    [TestMethod]
    public void Delete_ExistingId_RemovesActor()
    {
        Actor keanu = Repository.Add(NewKeanu());
        Repository.Add(new Actor { Name = "Carrie-Anne Moss" });

        Repository.Delete(keanu.Id);

        IRepository<Actor> fresh = FreshRepository();
        Assert.IsNull(fresh.Find(actor => actor.Id == keanu.Id));
        Assert.AreEqual(1, fresh.FindAll().Count);
    }

    [TestMethod]
    public void Delete_MissingId_DoesNothing()
    {
        Repository.Add(NewKeanu());

        Repository.Delete(999);

        Assert.AreEqual(1, FreshRepository().FindAll().Count);
    }

    private static Actor NewKeanu()
    {
        return new Actor { Name = "Keanu Reeves", Bio = "Canadian actor", BirthDate = KeanuBirthDate };
    }
}
