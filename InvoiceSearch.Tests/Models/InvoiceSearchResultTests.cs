using InvoiceSearch.Models;

namespace InvoiceSearch.Tests.Models;

public class InvoiceSearchResultTests
{
    [Fact]
    public void WhenCreatedWithDefaultsThenStringPropertiesAreEmpty()
    {
        var result = new InvoiceSearchResult();

        Assert.Equal(string.Empty, result.From);
        Assert.Equal(string.Empty, result.Subject);
        Assert.Equal(string.Empty, result.AttachmentName);
        Assert.Equal(string.Empty, result.DocumentText);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenNullablePropertiesAreNull()
    {
        var result = new InvoiceSearchResult();

        Assert.Null(result.InvoiceAmount);
        Assert.Null(result.InvoiceDate);
        Assert.Null(result.InvoiceIssuer);
        Assert.Null(result.ExportedDate);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenIsExcludedIsFalse()
    {
        var result = new InvoiceSearchResult();

        Assert.False(result.IsExcluded);
    }
}
