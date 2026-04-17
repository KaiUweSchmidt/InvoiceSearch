using InvoiceSearch.Data;
using InvoiceSearch.Models;
using Microsoft.Data.Sqlite;

namespace InvoiceSearch.Tests.Data;

public class DocumentCacheRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DocumentCacheRepository _repo;

    public DocumentCacheRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // DocumentCacheRepository needs the EmailAccounts table for foreign key
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS EmailAccounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DisplayName TEXT NOT NULL,
                EmailAddress TEXT NOT NULL,
                ImapServer TEXT NOT NULL,
                ImapPort INTEGER NOT NULL DEFAULT 993,
                UseSsl INTEGER NOT NULL DEFAULT 1,
                Username TEXT NOT NULL,
                EncryptedPassword BLOB NOT NULL
            );
            INSERT INTO EmailAccounts (Id, DisplayName, EmailAddress, ImapServer, Username, EncryptedPassword)
            VALUES (1, 'Test', 'test@example.com', 'imap.example.com', 'user', X'01');
            """;
        cmd.ExecuteNonQuery();

        _repo = new DocumentCacheRepository(_connection);
    }

    public void Dispose()
    {
        _repo.Dispose();
        _connection.Dispose();
    }

    private static CachedDocument CreateDoc(uint uid = 1, string attachmentName = "invoice.pdf") => new()
    {
        AccountId = 1,
        Uid = uid,
        AttachmentName = attachmentName,
        DocumentText = "Invoice text content",
        AttachmentData = [0xFF, 0xD8],
        From = "sender@example.com",
        ReceivedDate = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
        Subject = "Your Invoice",
        InvoiceAmount = 199.99m,
        InvoiceDate = "2024-06-15",
        InvoiceIssuer = "ACME GmbH"
    };

    [Fact]
    public void WhenNoCachedDocumentsThenGetAllCachedResultsReturnsEmpty()
    {
        var results = _repo.GetAllCachedResults();

        Assert.Empty(results);
    }

    [Fact]
    public void WhenDocumentSavedThenGetAllCachedResultsReturnsIt()
    {
        _repo.SaveCachedDocument(CreateDoc());

        var results = _repo.GetAllCachedResults();

        Assert.Single(results);
        Assert.Equal(1, results[0].AccountId);
        Assert.Equal(1u, results[0].Uid);
        Assert.Equal("sender@example.com", results[0].From);
        Assert.Equal("Your Invoice", results[0].Subject);
        Assert.Equal("invoice.pdf", results[0].AttachmentName);
        Assert.Equal(199.99m, results[0].InvoiceAmount);
        Assert.Equal("2024-06-15", results[0].InvoiceDate);
        Assert.Equal("ACME GmbH", results[0].InvoiceIssuer);
        Assert.False(results[0].IsExcluded);
        Assert.Null(results[0].ExportedDate);
    }

    [Fact]
    public void WhenDocumentSavedTwiceThenUpsertUpdatesFields()
    {
        _repo.SaveCachedDocument(CreateDoc());
        _repo.SaveCachedDocument(CreateDoc() with { InvoiceAmount = 300m, InvoiceIssuer = "New Corp" });

        var results = _repo.GetAllCachedResults();

        Assert.Single(results);
        Assert.Equal(300m, results[0].InvoiceAmount);
        Assert.Equal("New Corp", results[0].InvoiceIssuer);
    }

    [Fact]
    public void WhenGetDocumentTextCalledThenReturnsStoredText()
    {
        _repo.SaveCachedDocument(CreateDoc());

        var text = _repo.GetDocumentText(1, 1, "invoice.pdf");

        Assert.Equal("Invoice text content", text);
    }

    [Fact]
    public void WhenGetDocumentTextCalledForNonExistentThenReturnsNull()
    {
        var text = _repo.GetDocumentText(1, 999, "missing.pdf");

        Assert.Null(text);
    }

    [Fact]
    public void WhenGetAttachmentDataCalledThenReturnsStoredBytes()
    {
        _repo.SaveCachedDocument(CreateDoc());

        var data = _repo.GetAttachmentData(1, 1, "invoice.pdf");

        Assert.Equal(new byte[] { 0xFF, 0xD8 }, data);
    }

    [Fact]
    public void WhenGetAttachmentDataCalledForNonExistentThenReturnsNull()
    {
        var data = _repo.GetAttachmentData(1, 999, "missing.pdf");

        Assert.Null(data);
    }

    [Fact]
    public void WhenUpdateClassificationCalledThenFieldsAreUpdated()
    {
        _repo.SaveCachedDocument(CreateDoc());

        _repo.UpdateClassification(1, 1, "invoice.pdf", 500m, "2025-01-01", "New Issuer");
        var results = _repo.GetAllCachedResults();

        Assert.Equal(500m, results[0].InvoiceAmount);
        Assert.Equal("2025-01-01", results[0].InvoiceDate);
        Assert.Equal("New Issuer", results[0].InvoiceIssuer);
    }

    [Fact]
    public void WhenUpdateClassificationWithNullsThenFieldsAreCleared()
    {
        _repo.SaveCachedDocument(CreateDoc());

        _repo.UpdateClassification(1, 1, "invoice.pdf", null, null, null);
        var results = _repo.GetAllCachedResults();

        Assert.Null(results[0].InvoiceAmount);
        Assert.Null(results[0].InvoiceDate);
        Assert.Null(results[0].InvoiceIssuer);
    }

    [Fact]
    public void WhenRemoveCachedDocumentCalledThenDocumentIsDeleted()
    {
        _repo.SaveCachedDocument(CreateDoc());

        _repo.RemoveCachedDocument(1, 1, "invoice.pdf");

        Assert.Empty(_repo.GetAllCachedResults());
    }

    [Fact]
    public void WhenSetExcludedCalledThenIsExcludedIsUpdated()
    {
        _repo.SaveCachedDocument(CreateDoc());

        _repo.SetExcluded(1, 1, "invoice.pdf", true);
        var results = _repo.GetAllCachedResults();

        Assert.True(results[0].IsExcluded);
    }

    [Fact]
    public void WhenSetExcludedToFalseCalledThenIsExcludedIsCleared()
    {
        _repo.SaveCachedDocument(CreateDoc() with { IsExcluded = true });
        _repo.SetExcluded(1, 1, "invoice.pdf", false);

        var results = _repo.GetAllCachedResults();

        Assert.False(results[0].IsExcluded);
    }

    [Fact]
    public void WhenSetExportedDateCalledThenExportedDateIsSet()
    {
        _repo.SaveCachedDocument(CreateDoc());
        var exportDate = new DateTime(2024, 7, 1, 12, 0, 0, DateTimeKind.Utc);

        _repo.SetExportedDate(1, 1, "invoice.pdf", exportDate);
        var results = _repo.GetAllCachedResults();

        Assert.NotNull(results[0].ExportedDate);
    }

    [Fact]
    public void WhenInvalidateCacheCalledThenAllDocumentsForAccountAreDeleted()
    {
        _repo.SaveCachedDocument(CreateDoc(uid: 1));
        _repo.SaveCachedDocument(CreateDoc(uid: 2));

        _repo.InvalidateCache(1);

        Assert.Empty(_repo.GetAllCachedResults());
    }

    [Fact]
    public void WhenSaveCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _repo.SaveCachedDocument(null!));
    }

    [Fact]
    public void WhenConstructorCalledWithNullConnectionThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentCacheRepository(null!));
    }

    [Fact]
    public void WhenMultipleDocumentsSavedThenResultsAreOrderedByDateDescending()
    {
        _repo.SaveCachedDocument(CreateDoc(uid: 1) with
        {
            ReceivedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        _repo.SaveCachedDocument(CreateDoc(uid: 2) with
        {
            ReceivedDate = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var results = _repo.GetAllCachedResults();

        Assert.Equal(2, results.Count);
        Assert.True(results[0].ReceivedDate > results[1].ReceivedDate);
    }

    [Fact]
    public void WhenDocumentSavedWithNullOptionalFieldsThenNullsArePersisted()
    {
        _repo.SaveCachedDocument(CreateDoc() with
        {
            InvoiceAmount = null,
            InvoiceDate = null,
            InvoiceIssuer = null
        });

        var results = _repo.GetAllCachedResults();

        Assert.Single(results);
        Assert.Null(results[0].InvoiceAmount);
        Assert.Null(results[0].InvoiceDate);
        Assert.Null(results[0].InvoiceIssuer);
    }
}
