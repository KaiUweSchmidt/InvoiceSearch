using System.IO;
using InvoiceSearch.Models;
using Microsoft.Data.Sqlite;

namespace InvoiceSearch.Data;

/// <summary>
/// Provides CRUD operations for email accounts stored in a local SQLite database.
/// </summary>
public sealed class AccountRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    public AccountRepository()
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
            CREATE TABLE IF NOT EXISTS EmailAccounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DisplayName TEXT NOT NULL,
                EmailAddress TEXT NOT NULL,
                ImapServer TEXT NOT NULL,
                ImapPort INTEGER NOT NULL DEFAULT 993,
                UseSsl INTEGER NOT NULL DEFAULT 1,
                Username TEXT NOT NULL,
                EncryptedPassword BLOB NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns all stored email accounts.
    /// </summary>
    public List<EmailAccount> GetAll()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, DisplayName, EmailAddress, ImapServer, ImapPort, UseSsl, Username, EncryptedPassword
            FROM EmailAccounts
            ORDER BY DisplayName
            """;

        var accounts = new List<EmailAccount>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            accounts.Add(new EmailAccount
            {
                Id = reader.GetInt32(0),
                DisplayName = reader.GetString(1),
                EmailAddress = reader.GetString(2),
                ImapServer = reader.GetString(3),
                ImapPort = reader.GetInt32(4),
                UseSsl = reader.GetBoolean(5),
                Username = reader.GetString(6),
                EncryptedPassword = (byte[])reader[7]
            });
        }

        return accounts;
    }

    /// <summary>
    /// Inserts a new email account.
    /// </summary>
    public void Add(EmailAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO EmailAccounts (DisplayName, EmailAddress, ImapServer, ImapPort, UseSsl, Username, EncryptedPassword)
            VALUES (@displayName, @email, @server, @port, @ssl, @username, @password)
            """;
        AddAccountParameters(cmd, account);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates an existing email account by Id.
    /// </summary>
    public void Update(EmailAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE EmailAccounts SET
                DisplayName = @displayName,
                EmailAddress = @email,
                ImapServer = @server,
                ImapPort = @port,
                UseSsl = @ssl,
                Username = @username,
                EncryptedPassword = @password
            WHERE Id = @id
            """;
        cmd.Parameters.AddWithValue("@id", account.Id);
        AddAccountParameters(cmd, account);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes an email account by Id.
    /// </summary>
    public void Delete(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM EmailAccounts WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();

    private static void AddAccountParameters(SqliteCommand cmd, EmailAccount account)
    {
        cmd.Parameters.AddWithValue("@displayName", account.DisplayName);
        cmd.Parameters.AddWithValue("@email", account.EmailAddress);
        cmd.Parameters.AddWithValue("@server", account.ImapServer);
        cmd.Parameters.AddWithValue("@port", account.ImapPort);
        cmd.Parameters.AddWithValue("@ssl", account.UseSsl ? 1 : 0);
        cmd.Parameters.AddWithValue("@username", account.Username);
        cmd.Parameters.AddWithValue("@password", account.EncryptedPassword);
    }
}
