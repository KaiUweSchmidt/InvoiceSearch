namespace InvoiceSearch.Models;

/// <summary>
/// Represents a user-defined rule for fine-tuning invoice classification.
/// </summary>
public sealed record ClassificationRule
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public string Action { get; init; } = "Exclude";
}
