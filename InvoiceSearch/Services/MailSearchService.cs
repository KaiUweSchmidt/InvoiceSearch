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
/// using MailKit for IMAP access and ClassificationService for AI analysis.
/// </summary>
public sealed class MailSearchService
{
    private static readonly HashSet<string> s_pdfExtensions = [".pdf"];
    private static readonly HashSet<string> s_textExtensions = [".txt", ".csv", ".html", ".htm", ".xml"];

    /// <summary>
    /// Gets the highest UID processed during the last search call.
    /// </summary>
    public uint? LastProcessedUid { get; private set; }

    /// <summary>
    /// Gets the UidValidity of the inbox after opening.
    /// </summary>
    public uint? UidValidity { get; private set; }

    /// <summary>
    /// Indicates whether the UidValidity changed compared to the expected value.
    /// </summary>
    public bool UidValidityChanged { get; private set; }

    /// <summary>
    /// Searches the INBOX of the given account for emails with invoice attachments.
    /// Supports incremental search via <paramref name="startAfterUid"/> and
    /// automatic UidValidity change detection via <paramref name="expectedUidValidity"/>.
    /// </summary>
    public async IAsyncEnumerable<CachedDocument> SearchAccountAsync(
        EmailAccount account,
        ClassificationService classifier,
        IReadOnlyList<ClassificationRule>? classificationRules = null,
        uint? startAfterUid = null,
        uint? expectedUidValidity = null,
        IProgress<string>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(classifier);

        LastProcessedUid = null;
        UidValidityChanged = false;
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

        UidValidity = inbox.UidValidity;

        if (expectedUidValidity.HasValue && inbox.UidValidity != expectedUidValidity.Value)
        {
            UidValidityChanged = true;
            startAfterUid = null;
            progress?.Report("UidValidity geändert – suche alle E-Mails neu...");
        }

        var fetchItems = MessageSummaryItems.UniqueId
                       | MessageSummaryItems.Envelope
                       | MessageSummaryItems.BodyStructure;

        progress?.Report($"Lade Nachrichtenübersicht ({inbox.Count} E-Mails)...");
        var allSummaries = await inbox.FetchAsync(0, -1, fetchItems, cancellationToken);

        IList<IMessageSummary> summaries = startAfterUid.HasValue
            ? [.. allSummaries.Where(s => s.UniqueId.Id > startAfterUid.Value)]
            : allSummaries;

        progress?.Report(startAfterUid.HasValue
            ? $"{summaries.Count} neue E-Mails seit letzter Suche..."
            : $"{summaries.Count} E-Mails geladen...");

        var totalWithAttachments = 0;
        var processed = 0;

        foreach (var summary in summaries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastProcessedUid = summary.UniqueId.Id;

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

                string documentText;
                byte[] attachmentData;
                InvoiceAnalysis? analysis;
                try
                {
                    (documentText, attachmentData, analysis) = await AnalyzeAttachmentAsync(
                        inbox, summary.UniqueId, attachment, classifier, classificationRules, cancellationToken);
                }
                catch (Exception)
                {
                    continue;
                }

                if (analysis is not { IsInvoice: true })
                    continue;

                var fromAddress = summary.Envelope?.From?.Mailboxes.FirstOrDefault();
                var from = fromAddress is not null
                    ? (string.IsNullOrEmpty(fromAddress.Name) ? fromAddress.Address : $"{fromAddress.Name} <{fromAddress.Address}>")
                    : string.Empty;

                yield return new CachedDocument
                {
                    AccountId = account.Id,
                    Uid = summary.UniqueId.Id,
                    DocumentText = documentText,
                    AttachmentData = attachmentData,
                    From = from,
                    ReceivedDate = summary.Envelope?.Date?.LocalDateTime ?? DateTime.MinValue,
                    Subject = summary.Envelope?.Subject ?? string.Empty,
                    AttachmentName = fileName,
                    InvoiceAmount = analysis.Amount,
                    InvoiceDate = analysis.InvoiceDate,
                    InvoiceIssuer = analysis.Issuer
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

    private static async Task<(string DocumentText, byte[] AttachmentData, InvoiceAnalysis? Analysis)> AnalyzeAttachmentAsync(
        IMailFolder folder,
        UniqueId uid,
        BodyPartBasic attachment,
        ClassificationService classifier,
        IReadOnlyList<ClassificationRule>? classificationRules,
        CancellationToken cancellationToken)
    {
        var entity = await folder.GetBodyPartAsync(uid, attachment, cancellationToken);
        if (entity is not MimePart mimePart)
            return (string.Empty, [], null);

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
            documentText = System.Text.Encoding.UTF8.GetString(bytes);
        }

        if (string.IsNullOrWhiteSpace(documentText))
            return (string.Empty, bytes, null);

        var analysis = await classifier.AnalyzeDocumentTextAsync(documentText, classificationRules, cancellationToken);
        return (documentText, bytes, analysis);
    }
}
