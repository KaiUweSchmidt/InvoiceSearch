namespace InvoiceSearch.Models;

/// <summary>
/// Represents a single invoice found in an email attachment.
/// </summary>
public sealed record InvoiceSearchResult
{
    public string From { get; init; } = string.Empty;
    public DateTime ReceivedDate { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string AttachmentName { get; init; } = string.Empty;
    public decimal? InvoiceAmount { get; init; }
    public string? InvoiceDate { get; init; }
}
