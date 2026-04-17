using InvoiceSearch.Services;

namespace InvoiceSearch.Tests.Services;

public class PdfTextExtractorTests
{
    [Fact]
    public void WhenExtractTextCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PdfTextExtractor.ExtractText(null!));
    }
}
