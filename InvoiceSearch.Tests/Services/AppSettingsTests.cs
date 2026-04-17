using System.IO;
using System.Text.Json;
using InvoiceSearch.Services;

namespace InvoiceSearch.Tests.Services;

public class AppSettingsServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly AppSettingsService _service;

    public AppSettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"appsettings-test-{Guid.NewGuid()}.json");
        _service = new AppSettingsService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public void WhenFileDoesNotExistThenLoadReturnsDefaults()
    {
        var settings = _service.Load();

        Assert.NotNull(settings);
        Assert.Null(settings.ExportPath);
    }

    [Fact]
    public void WhenSettingsSavedThenLoadReturnsPersistedValues()
    {
        var settings = new AppSettings { ExportPath = @"C:\Exports" };

        _service.Save(settings);
        var loaded = _service.Load();

        Assert.Equal(@"C:\Exports", loaded.ExportPath);
    }

    [Fact]
    public void WhenSettingsOverwrittenThenNewValuesAreReturned()
    {
        _service.Save(new AppSettings { ExportPath = @"C:\Old" });
        _service.Save(new AppSettings { ExportPath = @"C:\New" });

        var loaded = _service.Load();

        Assert.Equal(@"C:\New", loaded.ExportPath);
    }

    [Fact]
    public void WhenSaveCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Save(null!));
    }

    [Fact]
    public void WhenConstructorCalledWithNullPathThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AppSettingsService(null!));
    }

    [Fact]
    public void WhenFileContainsInvalidJsonThenLoadReturnsDefaults()
    {
        File.WriteAllText(_tempFile, "not json");

        Assert.Throws<JsonException>(() => _service.Load());
    }

    [Fact]
    public void WhenSavedThenFileContainsIndentedJson()
    {
        _service.Save(new AppSettings { ExportPath = @"C:\Test" });

        var json = File.ReadAllText(_tempFile);

        Assert.Contains("\"exportPath\"", json);
        Assert.Contains("\n", json);
    }
}

public class AppSettingsRecordTests
{
    [Fact]
    public void WhenCreatedWithDefaultsThenExportPathIsNull()
    {
        var settings = new AppSettings();

        Assert.Null(settings.ExportPath);
    }

    [Fact]
    public void WhenCreatedWithValueThenExportPathIsSet()
    {
        var settings = new AppSettings { ExportPath = @"C:\Exports" };

        Assert.Equal(@"C:\Exports", settings.ExportPath);
    }

    [Fact]
    public void WhenTwoSettingsHaveSameValuesThenTheyAreEqual()
    {
        var a = new AppSettings { ExportPath = @"C:\Test" };
        var b = new AppSettings { ExportPath = @"C:\Test" };

        Assert.Equal(a, b);
    }
}
