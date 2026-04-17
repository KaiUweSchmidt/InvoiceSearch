using InvoiceSearch.Services;

namespace InvoiceSearch.Tests.Services;

public class AppSettingsTests
{
    [Fact]
    public void WhenAppSettingsCreatedWithDefaultsThenExportPathIsNull()
    {
        var settings = new AppSettings();

        Assert.Null(settings.ExportPath);
    }

    [Fact]
    public void WhenAppSettingsCreatedWithValueThenExportPathIsSet()
    {
        var settings = new AppSettings { ExportPath = @"C:\Exports" };

        Assert.Equal(@"C:\Exports", settings.ExportPath);
    }

    [Fact]
    public void WhenTwoAppSettingsHaveSameValuesThenTheyAreEqual()
    {
        var a = new AppSettings { ExportPath = @"C:\Test" };
        var b = new AppSettings { ExportPath = @"C:\Test" };

        Assert.Equal(a, b);
    }
}
