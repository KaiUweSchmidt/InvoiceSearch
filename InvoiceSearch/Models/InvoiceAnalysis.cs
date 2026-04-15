using System.Text.Json.Serialization;

namespace InvoiceSearch.Models;

/// <summary>
/// Represents the structured invoice analysis result returned by the LLM.
/// </summary>
public sealed record InvoiceAnalysis(
    [property: JsonPropertyName("isInvoice")] bool IsInvoice,
    [property: JsonPropertyName("amount")] decimal? Amount,
    [property: JsonPropertyName("invoiceDate")] string? InvoiceDate,
    [property: JsonPropertyName("issuer")] string? Issuer);
