using System.IO;
using InvoiceSearch.Models;
using Microsoft.Data.Sqlite;

namespace InvoiceSearch.Data;

/// <summary>
/// Provides cached document storage in SQLite with schema migration support.
/// </summary>
public sealed class DocumentCacheRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    public DocumentCacheRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "InvoiceSearch");
        Directory.CreateDirectory(folder);

        var dbPath = Path.Combine(folder, "accounts.db");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS CachedDocuments (
                AccountId INTEGER NOT NULL,
                Uid INTEGER NOT NULL,
                AttachmentName TEXT NOT NULL,
                DocumentText TEXT NOT NULL,
                AttachmentData BLOB NOT NULL DEFAULT X'',
                "From" TEXT NOT NULL DEFAULT '',
                ReceivedDate TEXT NOT NULL DEFAULT '',
                Subject TEXT NOT NULL DEFAULT '',
                InvoiceAmount REAL,
                InvoiceDate TEXT,
                InvoiceIssuer TEXT,
                IsExcluded INTEGER NOT NULL DEFAULT 0,
                ExportedDate TEXT,
                PRIMARY KEY (AccountId, Uid, AttachmentName),
                FOREIGN KEY (AccountId) REFERENCES EmailAccounts(Id) ON DELETE CASCADE
            )
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Saves or updates a cached document with all data.
    /// </summary>
    public void SaveCachedDocument(CachedDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO CachedDocuments
                (AccountId, Uid, AttachmentName, DocumentText, AttachmentData,
                 "From", ReceivedDate, Subject, InvoiceAmount, InvoiceDate, InvoiceIssuer, IsExcluded, ExportedDate)
            VALUES
                (@accountId, @uid, @attachmentName, @documentText, @attachmentData,
                 @from, @receivedDate, @subject, @amount, @invoiceDate, @issuer, @isExcluded, @exportedDate)
            ON CONFLICT(AccountId, Uid, AttachmentName) DO UPDATE SET
                DocumentText = @documentText,
                AttachmentData = @attachmentData,
                InvoiceAmount = @amount,
                InvoiceDate = @invoiceDate,
                InvoiceIssuer = @issuer
            """;
        cmd.Parameters.AddWithValue("@accountId", doc.AccountId);
        cmd.Parameters.AddWithValue("@uid", (long)doc.Uid);
        cmd.Parameters.AddWithValue("@attachmentName", doc.AttachmentName);
        cmd.Parameters.AddWithValue("@documentText", doc.DocumentText);
        cmd.Parameters.AddWithValue("@attachmentData", doc.AttachmentData);
        cmd.Parameters.AddWithValue("@from", doc.From);
        cmd.Parameters.AddWithValue("@receivedDate", doc.ReceivedDate.ToString("o"));
        cmd.Parameters.AddWithValue("@subject", doc.Subject);
        cmd.Parameters.AddWithValue("@amount", doc.InvoiceAmount.HasValue ? doc.InvoiceAmount.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@invoiceDate", doc.InvoiceDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@issuer", doc.InvoiceIssuer ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@isExcluded", doc.IsExcluded ? 1 : 0);
        cmd.Parameters.AddWithValue("@exportedDate", doc.ExportedDate.HasValue ? doc.ExportedDate.Value.ToString("o") : DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns all cached results as display DTOs ordered by date descending.
    /// </summary>
    public List<InvoiceSearchResult> GetAllCachedResults()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT AccountId, Uid, "From", ReceivedDate, Subject, AttachmentName,
                   InvoiceAmount, InvoiceDate, InvoiceIssuer, IsExcluded, ExportedDate
            FROM CachedDocuments
            ORDER BY ReceivedDate DESC
            """;

        var results = new List<InvoiceSearchResult>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new InvoiceSearchResult
            {
                AccountId = reader.GetInt32(0),
                Uid = (uint)reader.GetInt64(1),
                From = reader.GetString(2),
                ReceivedDate = DateTime.TryParse(reader.GetString(3), out var dt) ? dt : DateTime.MinValue,
                Subject = reader.GetString(4),
                AttachmentName = reader.GetString(5),
                InvoiceAmount = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                InvoiceDate = reader.IsDBNull(7) ? null : reader.GetString(7),
                InvoiceIssuer = reader.IsDBNull(8) ? null : reader.GetString(8),
                IsExcluded = reader.GetInt32(9) != 0,
                ExportedDate = reader.IsDBNull(10) ? null : DateTime.TryParse(reader.GetString(10), out var ed) ? ed : null
            });
        }

        return results;
    }

    /// <summary>
    /// Returns the document text for offline reclassification.
    /// </summary>
    public string? GetDocumentText(int accountId, uint uid, string attachmentName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT DocumentText FROM CachedDocuments
            WHERE AccountId = @accountId AND Uid = @uid AND AttachmentName = @attachmentName
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@uid", (long)uid);
        cmd.Parameters.AddWithValue("@attachmentName", attachmentName);
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// Returns the cached attachment data for export or opening.
    /// </summary>
    public byte[]? GetAttachmentData(int accountId, uint uid, string attachmentName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT AttachmentData FROM CachedDocuments
            WHERE AccountId = @accountId AND Uid = @uid AND AttachmentName = @attachmentName
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@uid", (long)uid);
        cmd.Parameters.AddWithValue("@attachmentName", attachmentName);
        return cmd.ExecuteScalar() as byte[];
    }

    /// <summary>
    /// Updates classification fields after reclassification.
    /// </summary>
    public void UpdateClassification(int accountId, uint uid, string attachmentName,
        decimal? amount, string? invoiceDate, string? issuer)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE CachedDocuments
            SET InvoiceAmount = @amount, InvoiceDate = @invoiceDate, InvoiceIssuer = @issuer
            WHERE AccountId = @accountId AND Uid = @uid AND AttachmentName = @attachmentName
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@uid", (long)uid);
        cmd.Parameters.AddWithValue("@attachmentName", attachmentName);
        cmd.Parameters.AddWithValue("@amount", amount.HasValue ? amount.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@invoiceDate", invoiceDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@issuer", issuer ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Removes a cached document that is no longer classified as an invoice.
    /// </summary>
    public void RemoveCachedDocument(int accountId, uint uid, string attachmentName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM CachedDocuments
            WHERE AccountId = @accountId AND Uid = @uid AND AttachmentName = @attachmentName
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@uid", (long)uid);
        cmd.Parameters.AddWithValue("@attachmentName", attachmentName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Sets the excluded state for a cached document.
    /// </summary>
    public void SetExcluded(int accountId, uint uid, string attachmentName, bool isExcluded)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE CachedDocuments SET IsExcluded = @isExcluded
            WHERE AccountId = @accountId AND Uid = @uid AND AttachmentName = @attachmentName
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@uid", (long)uid);
        cmd.Parameters.AddWithValue("@attachmentName", attachmentName);
        cmd.Parameters.AddWithValue("@isExcluded", isExcluded ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Records the export timestamp for a document.
    /// </summary>
    public void SetExportedDate(int accountId, uint uid, string attachmentName, DateTime exportedDate)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE CachedDocuments SET ExportedDate = @exportedDate
            WHERE AccountId = @accountId AND Uid = @uid AND AttachmentName = @attachmentName
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@uid", (long)uid);
        cmd.Parameters.AddWithValue("@attachmentName", attachmentName);
        cmd.Parameters.AddWithValue("@exportedDate", exportedDate.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Invalidates all cached documents for an account (used when UidValidity changes).
    /// </summary>
    public void InvalidateCache(int accountId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM CachedDocuments WHERE AccountId = @accountId";
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
