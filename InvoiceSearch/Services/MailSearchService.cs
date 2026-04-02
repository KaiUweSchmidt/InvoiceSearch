using System.IO;
using System.Runtime.CompilerServices;
using InvoiceSearch.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;

namespace InvoiceSearch.Services;

/// <summary>
/// Searches email accounts for attachments that contain invoices,
/// using MailKit for IMAP access and OllamaService for AI analysis.
/// </summary>
public sealed class MailSearchService
{
    private static readonly HashSet<string> s_pdfExtensions = [".pdf"];
    private static readonly HashSet<string> s_textExtensions = [".txt", ".csv", ".html", ".htm", ".xml"];

    /// <summary>
    /// Searches the INBOX of the given account for emails with invoice attachments.
    /// Yields results as they are found (streaming).
    /// </summary>
    public async IAsyncEnumerable<InvoiceSearchResult> SearchAccountAsync(
        EmailAccount account,
        OllamaService ollama,
        IProgress<string>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(ollama);

        var password = CredentialProtector.Unprotect(account.EncryptedPassword);

        using var client = new ImapClient();
        var socketOptions = account.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        progress?.Report($"Verbinde mit {account.ImapServer}...");
        await client.ConnectAsync(account.ImapServer, account.ImapPort, socketOptions, cancellationToken);
        await client.AuthenticateAsync(account.Username, password, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        progress?.Report($"Lade Nachrichtenübersicht ({inbox.Count} E-Mails)...");
        var items = MessageSummaryItems.UniqueId
                  | MessageSummaryItems.Envelope
                  | MessageSummaryItems.BodyStructure;
        var summaries = await inbox.FetchAsync(0, -1, items, cancellationToken);

        var totalWithAttachments = 0;
        var processed = 0;

        foreach (var summary in summaries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attachments = GetSupportedAttachments(summary);
            if (attachments.Count == 0)
                continue;

            totalWithAttachments++;

            foreach (var attachment in attachments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;

                var fileName = attachment.FileName ?? "unbekannt";
                progress?.Report($"Analysiere Anhang \"{fileName}\" ({processed})...");

                InvoiceAnalysis? analysis;
                try
                {
                    analysis = await AnalyzeAttachmentAsync(inbox, summary.UniqueId, attachment, ollama, cancellationToken);
                }
                catch (Exception)
                {
                    // Skip attachments that fail to download or analyze
                    continue;
                }

                if (analysis is not { IsInvoice: true })
                    continue;

                var fromAddress = summary.Envelope?.From?.Mailboxes.FirstOrDefault();
                var from = fromAddress is not null
                    ? (string.IsNullOrEmpty(fromAddress.Name) ? fromAddress.Address : $"{fromAddress.Name} <{fromAddress.Address}>")
                    : string.Empty;

                yield return new InvoiceSearchResult
                {
                    From = from,
                    ReceivedDate = summary.Envelope?.Date?.LocalDateTime ?? DateTime.MinValue,
                    Subject = summary.Envelope?.Subject ?? string.Empty,
                    AttachmentName = fileName,
                    InvoiceAmount = analysis.Amount,
                    InvoiceDate = analysis.InvoiceDate
                };
            }
        }

        await client.DisconnectAsync(quit: true, cancellationToken);
        progress?.Report($"Konto \"{account.DisplayName}\" abgeschlossen – {totalWithAttachments} E-Mails mit Anhängen geprüft.");
    }

    private static List<BodyPartBasic> GetSupportedAttachments(IMessageSummary summary)
    {
        var result = new List<BodyPartBasic>();

        foreach (var part in summary.Attachments)
        {
            var ext = Path.GetExtension(part.FileName)?.ToLowerInvariant();
            if (ext is not null && (s_pdfExtensions.Contains(ext) || s_textExtensions.Contains(ext)))
            {
                result.Add(part);
            }
        }

        return result;
    }

    private static async Task<InvoiceAnalysis?> AnalyzeAttachmentAsync(
        IMailFolder folder,
        UniqueId uid,
        BodyPartBasic attachment,
        OllamaService ollama,
        CancellationToken cancellationToken)
    {
        var entity = await folder.GetBodyPartAsync(uid, attachment, cancellationToken);
        if (entity is not MimePart mimePart)
            return null;

        using var ms = new MemoryStream();
        await mimePart.Content.DecodeToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var ext = Path.GetExtension(attachment.FileName)?.ToLowerInvariant();
        string documentText;

        if (s_pdfExtensions.Contains(ext!))
        {
            documentText = PdfTextExtractor.ExtractText(bytes);
        }
        else
        {
            // Text-based attachment (txt, csv, html, xml)
            documentText = System.Text.Encoding.UTF8.GetString(bytes);
        }

        if (string.IsNullOrWhiteSpace(documentText))
            return null;

        return await ollama.AnalyzeDocumentTextAsync(documentText, cancellationToken);
    }
}
