using MailKit.Net.Imap;
using MailKit.Security;

namespace InvoiceSearch.Services;

/// <summary>
/// Tests IMAP connectivity for email accounts using MailKit.
/// </summary>
public static class ImapTestService
{
    /// <summary>
    /// Tests the IMAP connection by connecting, authenticating, and disconnecting.
    /// Throws on failure so the caller can display the error message.
    /// </summary>
    public static async Task TestConnectionAsync(
        string imapServer,
        int imapPort,
        bool useSsl,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();

        var socketOptions = useSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(imapServer, imapPort, socketOptions, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
