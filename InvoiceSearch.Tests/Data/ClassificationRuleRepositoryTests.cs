using InvoiceSearch.Data;
using InvoiceSearch.Models;
using Microsoft.Data.Sqlite;

namespace InvoiceSearch.Tests.Data;

public class ClassificationRuleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ClassificationRuleRepository _repo;

    public ClassificationRuleRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _repo = new ClassificationRuleRepository(_connection);
    }

    public void Dispose()
    {
        _repo.Dispose();
        _connection.Dispose();
    }

    private static ClassificationRule CreateRule(string name = "Insurance", string pattern = "Versicherung") => new()
    {
        Name = name,
        Pattern = pattern,
        Action = "Exclude"
    };

    [Fact]
    public void WhenNoRulesAddedThenGetAllReturnsEmpty()
    {
        var rules = _repo.GetAll();

        Assert.Empty(rules);
    }

    [Fact]
    public void WhenRuleAddedThenGetAllReturnsIt()
    {
        _repo.Add(CreateRule());

        var rules = _repo.GetAll();

        Assert.Single(rules);
        Assert.Equal("Insurance", rules[0].Name);
        Assert.Equal("Versicherung", rules[0].Pattern);
        Assert.Equal("Exclude", rules[0].Action);
    }

    [Fact]
    public void WhenMultipleRulesAddedThenGetAllReturnsSortedByName()
    {
        _repo.Add(CreateRule(name: "Zebra"));
        _repo.Add(CreateRule(name: "Alpha"));

        var rules = _repo.GetAll();

        Assert.Equal(2, rules.Count);
        Assert.Equal("Alpha", rules[0].Name);
        Assert.Equal("Zebra", rules[1].Name);
    }

    [Fact]
    public void WhenRuleUpdatedThenChangesArePersisted()
    {
        _repo.Add(CreateRule());
        var added = _repo.GetAll()[0];
        var updated = added with { Name = "Updated", Pattern = "new-pattern", Action = "Include" };

        _repo.Update(updated);
        var result = _repo.GetAll()[0];

        Assert.Equal("Updated", result.Name);
        Assert.Equal("new-pattern", result.Pattern);
        Assert.Equal("Include", result.Action);
    }

    [Fact]
    public void WhenRuleDeletedThenItIsRemoved()
    {
        _repo.Add(CreateRule());
        var id = _repo.GetAll()[0].Id;

        _repo.Delete(id);

        Assert.Empty(_repo.GetAll());
    }

    [Fact]
    public void WhenAddCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _repo.Add(null!));
    }

    [Fact]
    public void WhenUpdateCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _repo.Update(null!));
    }

    [Fact]
    public void WhenConstructorCalledWithNullConnectionThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ClassificationRuleRepository(null!));
    }
}
