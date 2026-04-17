using InvoiceSearch.Models;

namespace InvoiceSearch.Tests.Models;

public class CachedDocumentTests
{
    [Fact]
    public void WhenCreatedWithDefaultsThenStringPropertiesAreEmpty()
    {
        var doc = new CachedDocument();

        Assert.Equal(string.Empty, doc.AttachmentName);
        Assert.Equal(string.Empty, doc.DocumentText);
        Assert.Equal(string.Empty, doc.From);
        Assert.Equal(string.Empty, doc.Subject);
        Assert.Empty(doc.AttachmentData);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenNullablePropertiesAreNull()
    {
        var doc = new CachedDocument();

        Assert.Null(doc.InvoiceAmount);
        Assert.Null(doc.InvoiceDate);
        Assert.Null(doc.InvoiceIssuer);
        Assert.Null(doc.ExportedDate);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenIsExcludedIsFalse()
    {
        var doc = new CachedDocument();

        Assert.False(doc.IsExcluded);
    }

    [Fact]
    public void WhenToSearchResultCalledThenAllPropertiesAreMapped()
    {
        var now = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var exported = new DateTime(2024, 6, 16, 12, 0, 0, DateTimeKind.Utc);
        var doc = new CachedDocument
        {
            AccountId = 1,
            Uid = 42,
            From = "sender@example.com",
            ReceivedDate = now,
            Subject = "Invoice",
            AttachmentName = "invoice.pdf",
            InvoiceAmount = 199.99m,
            InvoiceDate = "2024-06-15",
            InvoiceIssuer = "ACME GmbH",
            IsExcluded = true,
            ExportedDate = exported,
            DocumentText = "Some text"
        };

        var result = doc.ToSearchResult();

        Assert.Equal(1, result.AccountId);
        Assert.Equal(42u, result.Uid);
        Assert.Equal("sender@example.com", result.From);
        Assert.Equal(now, result.ReceivedDate);
        Assert.Equal("Invoice", result.Subject);
        Assert.Equal("invoice.pdf", result.AttachmentName);
        Assert.Equal(199.99m, result.InvoiceAmount);
        Assert.Equal("2024-06-15", result.InvoiceDate);
        Assert.Equal("ACME GmbH", result.InvoiceIssuer);
        Assert.True(result.IsExcluded);
        Assert.Equal(exported, result.ExportedDate);
        Assert.Equal("Some text", result.DocumentText);
    }

    [Fact]
    public void WhenToSearchResultCalledWithNullablesThenNullsAreMapped()
    {
        var doc = new CachedDocument
        {
            AccountId = 1,
            Uid = 1,
            AttachmentName = "test.pdf"
        };

        var result = doc.ToSearchResult();

        Assert.Null(result.InvoiceAmount);
        Assert.Null(result.InvoiceDate);
        Assert.Null(result.InvoiceIssuer);
        Assert.Null(result.ExportedDate);
        Assert.False(result.IsExcluded);
    }
}
