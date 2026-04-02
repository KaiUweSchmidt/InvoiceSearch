using System.Text;
using UglyToad.PdfPig;

namespace InvoiceSearch.Services;

/// <summary>
/// Extracts text content from PDF byte arrays using PdfPig.
/// </summary>
public static class PdfTextExtractor
{
    /// <summary>
    /// Extracts all text from a PDF document.
    /// Returns an empty string if extraction fails or the PDF contains no text.
    /// </summary>
    public static string ExtractText(byte[] pdfData)
    {
        ArgumentNullException.ThrowIfNull(pdfData);

        using var document = PdfDocument.Open(pdfData);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }
}
