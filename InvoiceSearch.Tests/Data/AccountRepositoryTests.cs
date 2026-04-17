using InvoiceSearch.Data;
using InvoiceSearch.Models;
using Microsoft.Data.Sqlite;

namespace InvoiceSearch.Tests.Data;

public class AccountRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AccountRepository _repo;

    public AccountRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Enable foreign keys and create dependent tables that AccountRepository.Delete references
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            PRAGMA foreign_keys = ON;
            CREATE TABLE IF NOT EXISTS CachedDocuments (
                AccountId INTEGER NOT NULL,
                Uid INTEGER NOT NULL,
                AttachmentName TEXT NOT NULL,
                PRIMARY KEY (AccountId, Uid, AttachmentName)
            );
            """;
        cmd.ExecuteNonQuery();

        _repo = new AccountRepository(_connection);
    }

    public void Dispose()
    {
        _repo.Dispose();
        _connection.Dispose();
    }

    private static EmailAccount CreateAccount(int id = 0, string displayName = "Test") => new()
    {
        Id = id,
        DisplayName = displayName,
        EmailAddress = "test@example.com",
        ImapServer = "imap.example.com",
        ImapPort = 993,
        UseSsl = true,
        Username = "user",
        EncryptedPassword = [0x01, 0x02, 0x03]
    };

    [Fact]
    public void WhenNoAccountsAddedThenGetAllReturnsEmpty()
    {
        var accounts = _repo.GetAll();

        Assert.Empty(accounts);
    }

    [Fact]
    public void WhenAccountAddedThenGetAllReturnsIt()
    {
        _repo.Add(CreateAccount());

        var accounts = _repo.GetAll();

        Assert.Single(accounts);
        Assert.Equal("Test", accounts[0].DisplayName);
        Assert.Equal("test@example.com", accounts[0].EmailAddress);
        Assert.Equal("imap.example.com", accounts[0].ImapServer);
        Assert.Equal(993, accounts[0].ImapPort);
        Assert.True(accounts[0].UseSsl);
        Assert.Equal("user", accounts[0].Username);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, accounts[0].EncryptedPassword);
    }

    [Fact]
    public void WhenMultipleAccountsAddedThenGetAllReturnsSortedByDisplayName()
    {
        _repo.Add(CreateAccount(displayName: "Zebra"));
        _repo.Add(CreateAccount(displayName: "Alpha"));

        var accounts = _repo.GetAll();

        Assert.Equal(2, accounts.Count);
        Assert.Equal("Alpha", accounts[0].DisplayName);
        Assert.Equal("Zebra", accounts[1].DisplayName);
    }

    [Fact]
    public void WhenAccountUpdatedThenChangesArePersisted()
    {
        _repo.Add(CreateAccount());
        var added = _repo.GetAll()[0];
        var updated = added with { DisplayName = "Updated", ImapPort = 143, UseSsl = false };

        _repo.Update(updated);
        var result = _repo.GetAll()[0];

        Assert.Equal("Updated", result.DisplayName);
        Assert.Equal(143, result.ImapPort);
        Assert.False(result.UseSsl);
    }

    [Fact]
    public void WhenAccountDeletedThenItIsRemoved()
    {
        _repo.Add(CreateAccount());
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
    public void WhenNoSearchStateThenGetLastSearchedUidReturnsNull()
    {
        var uid = _repo.GetLastSearchedUid(1);

        Assert.Null(uid);
    }

    [Fact]
    public void WhenSearchStateSetThenGetLastSearchedUidReturnsValue()
    {
        _repo.Add(CreateAccount());
        var id = _repo.GetAll()[0].Id;
        _repo.SetLastSearchedUid(id, 42);

        var uid = _repo.GetLastSearchedUid(id);

        Assert.Equal(42u, uid);
    }

    [Fact]
    public void WhenSearchStateUpdatedThenNewValueIsReturned()
    {
        _repo.Add(CreateAccount());
        var id = _repo.GetAll()[0].Id;
        _repo.SetLastSearchedUid(id, 10);
        _repo.SetLastSearchedUid(id, 50);

        var uid = _repo.GetLastSearchedUid(id);

        Assert.Equal(50u, uid);
    }

    [Fact]
    public void WhenNoUidValidityThenGetUidValidityReturnsNull()
    {
        var validity = _repo.GetUidValidity(1);

        Assert.Null(validity);
    }

    [Fact]
    public void WhenUidValiditySetThenGetUidValidityReturnsValue()
    {
        _repo.Add(CreateAccount());
        var id = _repo.GetAll()[0].Id;
        _repo.SetUidValidity(id, 12345);

        var validity = _repo.GetUidValidity(id);

        Assert.Equal(12345u, validity);
    }

    [Fact]
    public void WhenUidValidityUpdatedThenNewValueIsReturned()
    {
        _repo.Add(CreateAccount());
        var id = _repo.GetAll()[0].Id;
        _repo.SetUidValidity(id, 100);
        _repo.SetUidValidity(id, 200);

        var validity = _repo.GetUidValidity(id);

        Assert.Equal(200u, validity);
    }

    [Fact]
    public void WhenConstructorCalledWithNullConnectionThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AccountRepository(null!));
    }
}
