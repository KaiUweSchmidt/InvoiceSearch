using System.IO;
using InvoiceSearch.Models;
using Microsoft.Data.Sqlite;

namespace InvoiceSearch.Data;

/// <summary>
/// Provides CRUD operations for user-defined classification rules.
/// </summary>
public sealed class ClassificationRuleRepository : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly bool _ownsConnection;

    public ClassificationRuleRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "InvoiceSearch");
        Directory.CreateDirectory(folder);

        var dbPath = Path.Combine(folder, "accounts.db");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        _ownsConnection = true;
        EnsureSchema();
    }

    /// <summary>
    /// Creates a repository using an existing open connection (for testing).
    /// </summary>
    public ClassificationRuleRepository(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _ownsConnection = false;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ClassificationRules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Pattern TEXT NOT NULL,
                Action TEXT NOT NULL DEFAULT 'Exclude'
            )
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns all classification rules.
    /// </summary>
    public List<ClassificationRule> GetAll()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Pattern, Action FROM ClassificationRules ORDER BY Name";

        var rules = new List<ClassificationRule>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rules.Add(new ClassificationRule
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Pattern = reader.GetString(2),
                Action = reader.GetString(3)
            });
        }

        return rules;
    }

    /// <summary>
    /// Inserts a new classification rule.
    /// </summary>
    public void Add(ClassificationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ClassificationRules (Name, Pattern, Action)
            VALUES (@name, @pattern, @action)
            """;
        cmd.Parameters.AddWithValue("@name", rule.Name);
        cmd.Parameters.AddWithValue("@pattern", rule.Pattern);
        cmd.Parameters.AddWithValue("@action", rule.Action);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates an existing classification rule.
    /// </summary>
    public void Update(ClassificationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE ClassificationRules SET Name = @name, Pattern = @pattern, Action = @action
            WHERE Id = @id
            """;
        cmd.Parameters.AddWithValue("@id", rule.Id);
        cmd.Parameters.AddWithValue("@name", rule.Name);
        cmd.Parameters.AddWithValue("@pattern", rule.Pattern);
        cmd.Parameters.AddWithValue("@action", rule.Action);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes a classification rule by Id.
    /// </summary>
    public void Delete(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ClassificationRules WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_ownsConnection)
            _connection.Dispose();
    }
}
