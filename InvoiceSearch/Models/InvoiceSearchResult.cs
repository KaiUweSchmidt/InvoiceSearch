namespace InvoiceSearch.Models;

/// <summary>
/// Display DTO for the DataGrid representing a found invoice.
/// </summary>
public sealed record InvoiceSearchResult
{
    public int AccountId { get; init; }
    public uint Uid { get; init; }
    public string From { get; init; } = string.Empty;
    public DateTime ReceivedDate { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string AttachmentName { get; init; } = string.Empty;
    public decimal? InvoiceAmount { get; init; }
    public string? InvoiceDate { get; init; }
    public string? InvoiceIssuer { get; init; }
    public bool IsExcluded { get; init; }
    public DateTime? ExportedDate { get; init; }
    public string DocumentText { get; init; } = string.Empty;
}
