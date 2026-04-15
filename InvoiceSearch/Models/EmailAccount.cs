namespace InvoiceSearch.Models;

/// <summary>
/// Represents an email account configuration for IMAP access.
/// </summary>
public sealed record EmailAccount
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string ImapServer { get; init; } = string.Empty;
    public int ImapPort { get; init; } = 993;
    public bool UseSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public byte[] EncryptedPassword { get; init; } = [];
}
