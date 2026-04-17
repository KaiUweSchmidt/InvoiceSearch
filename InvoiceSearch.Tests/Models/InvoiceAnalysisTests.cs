using System.Text.Json;
using InvoiceSearch.Models;

namespace InvoiceSearch.Tests.Models;

public class InvoiceAnalysisTests
{
    [Fact]
    public void WhenDeserializingInvoiceJsonThenAllPropertiesAreParsed()
    {
        var json = """{"isInvoice": true, "amount": 123.45, "invoiceDate": "2024-01-15", "issuer": "ACME GmbH"}""";

        var result = JsonSerializer.Deserialize<InvoiceAnalysis>(json);

        Assert.NotNull(result);
        Assert.True(result.IsInvoice);
        Assert.Equal(123.45m, result.Amount);
        Assert.Equal("2024-01-15", result.InvoiceDate);
        Assert.Equal("ACME GmbH", result.Issuer);
    }

    [Fact]
    public void WhenDeserializingNonInvoiceJsonThenNullablePropertiesAreNull()
    {
        var json = """{"isInvoice": false, "amount": null, "invoiceDate": null, "issuer": null}""";

        var result = JsonSerializer.Deserialize<InvoiceAnalysis>(json);

        Assert.NotNull(result);
        Assert.False(result.IsInvoice);
        Assert.Null(result.Amount);
        Assert.Null(result.InvoiceDate);
        Assert.Null(result.Issuer);
    }

    [Fact]
    public void WhenSerializingThenJsonPropertyNamesAreUsed()
    {
        var analysis = new InvoiceAnalysis(true, 99.99m, "2024-12-01", "Test Corp");

        var json = JsonSerializer.Serialize(analysis);

        Assert.Contains("\"isInvoice\":true", json);
        Assert.Contains("\"amount\":99.99", json);
        Assert.Contains("\"invoiceDate\":\"2024-12-01\"", json);
        Assert.Contains("\"issuer\":\"Test Corp\"", json);
    }

    [Fact]
    public void WhenTwoAnalysesHaveSameValuesThenTheyAreEqual()
    {
        var a = new InvoiceAnalysis(true, 100m, "2024-01-01", "Test");
        var b = new InvoiceAnalysis(true, 100m, "2024-01-01", "Test");

        Assert.Equal(a, b);
    }
}
