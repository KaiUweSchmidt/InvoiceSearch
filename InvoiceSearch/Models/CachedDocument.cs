namespace InvoiceSearch.Models;

/// <summary>
/// Represents a cached email attachment with extracted text, raw data, and classification results.
/// </summary>
public sealed record CachedDocument
{
    public int AccountId { get; init; }
    public uint Uid { get; init; }
    public string AttachmentName { get; init; } = string.Empty;
    public string DocumentText { get; init; } = string.Empty;
    public byte[] AttachmentData { get; init; } = [];
    public string From { get; init; } = string.Empty;
    public DateTime ReceivedDate { get; init; }
    public string Subject { get; init; } = string.Empty;
    public decimal? InvoiceAmount { get; init; }
    public string? InvoiceDate { get; init; }
    public string? InvoiceIssuer { get; init; }
    public bool IsExcluded { get; init; }
    public DateTime? ExportedDate { get; init; }

    /// <summary>
    /// Converts this cached document to a display DTO for the DataGrid.
    /// </summary>
    public InvoiceSearchResult ToSearchResult() => new()
    {
        AccountId = AccountId,
        Uid = Uid,
        From = From,
        ReceivedDate = ReceivedDate,
        Subject = Subject,
        AttachmentName = AttachmentName,
        InvoiceAmount = InvoiceAmount,
        InvoiceDate = InvoiceDate,
        InvoiceIssuer = InvoiceIssuer,
        IsExcluded = IsExcluded,
        ExportedDate = ExportedDate,
        DocumentText = DocumentText
    };
}
