using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceSearch.Models;

namespace InvoiceSearch.Services;

/// <summary>
/// Communicates with a local Ollama instance to analyze document text for invoices.
/// </summary>
public sealed class OllamaService
{
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };

    private const string DefaultEndpoint = "http://localhost:11434/api/generate";
    private const string DefaultModel = "llama3";

    private static readonly string s_systemPrompt = """
        You are a document analyst. Analyze the following document text.
        Determine whether it is an invoice (Rechnung). The document may be in any language (German, English, or other).
        If it is an invoice, extract the total gross amount and the invoice date.

        Respond ONLY with a JSON object, no additional text.

        Format for an invoice:
        {"isInvoice": true, "amount": 123.45, "invoiceDate": "2024-01-15"}

        Format for a non-invoice:
        {"isInvoice": false, "amount": null, "invoiceDate": null}

        Rules:
        - "amount" must be a number (decimal), not a string.
        - "invoiceDate" must be in ISO 8601 format (yyyy-MM-dd).
        - Use the total/gross amount including tax if available.
        """;

    private readonly string _endpoint;
    private readonly string _model;

    public OllamaService(string endpoint = DefaultEndpoint, string model = DefaultModel)
    {
        _endpoint = endpoint;
        _model = model;
    }

    /// <summary>
    /// Sends extracted document text to the LLM for invoice analysis.
    /// Returns <c>null</c> if the response cannot be parsed.
    /// </summary>
    public async Task<InvoiceAnalysis?> AnalyzeDocumentTextAsync(
        string documentText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            return null;

        var prompt = $"{s_systemPrompt}\n\nDokumenttext:\n---\n{documentText}\n---";

        var request = new OllamaRequest
        {
            Model = _model,
            Prompt = prompt,
            Stream = false,
            Format = "json"
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await s_httpClient.PostAsync(_endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var ollamaResponse = await JsonSerializer.DeserializeAsync<OllamaResponse>(stream, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(ollamaResponse?.Response))
            return null;

        return JsonSerializer.Deserialize<InvoiceAnalysis>(ollamaResponse.Response);
    }

    private sealed record OllamaRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("format")]
        public string Format { get; init; } = "json";
    }

    private sealed record OllamaResponse(
        [property: JsonPropertyName("response")] string? Response,
        [property: JsonPropertyName("done")] bool Done);
}
