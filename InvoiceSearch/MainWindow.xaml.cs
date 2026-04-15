using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using InvoiceSearch.Data;
using InvoiceSearch.Models;
using InvoiceSearch.Services;

namespace InvoiceSearch;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly string[] s_germanMonths =
        ["Januar", "Februar", "März", "April", "Mai", "Juni",
         "Juli", "August", "September", "Oktober", "November", "Dezember"];

    private readonly ObservableCollection<InvoiceSearchResult> _results = [];
    private ICollectionView _view = null!;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        WindowPlacementService.Restore(this);

        _view = CollectionViewSource.GetDefaultView(_results);
        _view.Filter = FilterResult;
        ResultsGrid.ItemsSource = _view;

        LoadCachedResults();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        WindowPlacementService.Save(this);
        base.OnClosing(e);
    }

    private bool FilterResult(object obj)
    {
        if (obj is not InvoiceSearchResult result) return false;
        if (HideExcludedBox.IsChecked == true && result.IsExcluded) return false;
        if (HideExportedBox.IsChecked == true && result.ExportedDate.HasValue) return false;
        return true;
    }

    private void LoadCachedResults()
    {
        using var cacheRepo = new DocumentCacheRepository();
        var cached = cacheRepo.GetAllCachedResults();

        foreach (var result in cached)
        {
            _results.Add(result);
        }

        if (_results.Count > 0)
        {
            StatusText.Text = $"{_results.Count} Rechnung(en) aus Cache geladen.";
            UpdateButtonStates();
        }
    }

    private async void OnSearchClick(object sender, RoutedEventArgs e)
    {
        using var accountRepo = new AccountRepository();
        var accounts = accountRepo.GetAll();

        if (accounts.Count == 0)
        {
            MessageBox.Show(
                "Keine E-Mail-Konten konfiguriert.\nBitte zuerst unter Datei → Konten ein Konto anlegen.",
                "Keine Konten",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _cts = new CancellationTokenSource();
        SetSearchingState(true);

        var progress = new Progress<string>(message => StatusText.Text = message);
        var classifier = new ClassificationService();
        using var cacheRepo = new DocumentCacheRepository();
        using var ruleRepo = new ClassificationRuleRepository();
        var rules = ruleRepo.GetAll();

        try
        {
            foreach (var account in accounts)
            {
                var lastUid = accountRepo.GetLastSearchedUid(account.Id);
                var uidValidity = accountRepo.GetUidValidity(account.Id);
                var searchService = new MailSearchService();

                try
                {
                    await foreach (var doc in searchService.SearchAccountAsync(
                        account, classifier, rules, lastUid, uidValidity, progress, _cts.Token))
                    {
                        cacheRepo.SaveCachedDocument(doc);
                        _results.Add(doc.ToSearchResult());
                    }
                }
                finally
                {
                    if (searchService.UidValidityChanged)
                    {
                        cacheRepo.InvalidateCache(account.Id);
                        for (var i = _results.Count - 1; i >= 0; i--)
                        {
                            if (_results[i].AccountId == account.Id)
                                _results.RemoveAt(i);
                        }
                    }

                    if (searchService.LastProcessedUid.HasValue)
                    {
                        accountRepo.SetLastSearchedUid(account.Id, searchService.LastProcessedUid.Value);
                    }

                    if (searchService.UidValidity.HasValue)
                    {
                        accountRepo.SetUidValidity(account.Id, searchService.UidValidity.Value);
                    }
                }
            }

            StatusText.Text = $"Suche abgeschlossen – {_results.Count} Rechnung(en) gefunden.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = $"Suche abgebrochen – {_results.Count} Rechnung(en) bisher gefunden.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler bei der Suche:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText.Text = "Fehler bei der Suche.";
        }
        finally
        {
            SetSearchingState(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    private async void OnReclassifyClick(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
            return;

        _cts = new CancellationTokenSource();
        SetSearchingState(true);

        var classifier = new ClassificationService();
        using var cacheRepo = new DocumentCacheRepository();
        using var ruleRepo = new ClassificationRuleRepository();
        var rules = ruleRepo.GetAll();
        var currentResults = _results.ToList();

        try
        {
            _results.Clear();
            var total = currentResults.Count;

            for (var i = 0; i < total; i++)
            {
                var result = currentResults[i];
                _cts.Token.ThrowIfCancellationRequested();
                StatusText.Text = $"Klassifiziere \"{result.AttachmentName}\" neu ({i + 1}/{total})...";

                var documentText = cacheRepo.GetDocumentText(
                    result.AccountId, result.Uid, result.AttachmentName);

                if (string.IsNullOrWhiteSpace(documentText))
                {
                    _results.Add(result);
                    continue;
                }

                InvoiceAnalysis? analysis = null;
                try
                {
                    analysis = await classifier.AnalyzeDocumentTextAsync(documentText, rules, _cts.Token);
                }
                catch
                {
                    _results.Add(result);
                    continue;
                }

                if (analysis is not { IsInvoice: true })
                {
                    cacheRepo.RemoveCachedDocument(result.AccountId, result.Uid, result.AttachmentName);
                    continue;
                }

                var updated = result with
                {
                    InvoiceAmount = analysis.Amount,
                    InvoiceDate = analysis.InvoiceDate,
                    InvoiceIssuer = analysis.Issuer
                };

                cacheRepo.UpdateClassification(
                    result.AccountId, result.Uid, result.AttachmentName,
                    analysis.Amount, analysis.InvoiceDate, analysis.Issuer);
                _results.Add(updated);
            }

            StatusText.Text = $"Neuklassifizierung abgeschlossen – {_results.Count} Rechnung(en).";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = $"Neuklassifizierung abgebrochen – {_results.Count} Rechnung(en) bisher.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fehler bei der Neuklassifizierung:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText.Text = "Fehler bei der Neuklassifizierung.";
        }
        finally
        {
            SetSearchingState(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        var settings = AppSettingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.ExportPath))
        {
            MessageBox.Show(
                "Bitte zuerst einen Ablageort festlegen (Datei → Ablageort festlegen…).",
                "Kein Ablageort",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        using var cacheRepo = new DocumentCacheRepository();
        var exportTime = DateTime.Now;
        var exported = 0;

        var toExport = _results
            .Where(r => !r.IsExcluded && !r.ExportedDate.HasValue)
            .ToList();

        foreach (var result in toExport)
        {
            var data = cacheRepo.GetAttachmentData(result.AccountId, result.Uid, result.AttachmentName);
            if (data is null || data.Length == 0)
                continue;

            var monthDir = GetMonthDirectory(result.InvoiceDate);
            var targetDir = Path.Combine(settings.ExportPath, monthDir);
            Directory.CreateDirectory(targetDir);

            var targetPath = GetUniqueFilePath(Path.Combine(targetDir, result.AttachmentName));
            File.WriteAllBytes(targetPath, data);

            cacheRepo.SetExportedDate(result.AccountId, result.Uid, result.AttachmentName, exportTime);

            var index = _results.IndexOf(result);
            if (index >= 0)
            {
                _results[index] = result with { ExportedDate = exportTime };
            }

            exported++;
        }

        _view.Refresh();
        StatusText.Text = $"{exported} Rechnung(en) exportiert nach \"{settings.ExportPath}\".";
    }

    private void OnExcludeClick(object sender, RoutedEventArgs e) => SetSelectedExcluded(true);

    private void OnIncludeClick(object sender, RoutedEventArgs e) => SetSelectedExcluded(false);

    private void SetSelectedExcluded(bool exclude)
    {
        var selected = ResultsGrid.SelectedItems.Cast<InvoiceSearchResult>().ToList();
        if (selected.Count == 0)
            return;

        using var cacheRepo = new DocumentCacheRepository();
        foreach (var result in selected)
        {
            cacheRepo.SetExcluded(result.AccountId, result.Uid, result.AttachmentName, exclude);

            var index = _results.IndexOf(result);
            if (index >= 0)
            {
                _results[index] = result with { IsExcluded = exclude };
            }
        }

        _view.Refresh();
        StatusText.Text = exclude
            ? $"{selected.Count} Dokument(e) ausgeschlossen."
            : $"{selected.Count} Dokument(e) eingeschlossen.";
    }

    private void OnOpenAttachmentClick(object sender, RoutedEventArgs e)
    {
        if (ResultsGrid.SelectedItem is not InvoiceSearchResult result)
            return;

        using var cacheRepo = new DocumentCacheRepository();
        var data = cacheRepo.GetAttachmentData(result.AccountId, result.Uid, result.AttachmentName);

        if (data is null || data.Length == 0)
        {
            MessageBox.Show(
                "Anhang nicht im Cache verfügbar.",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "InvoiceSearch");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, result.AttachmentName);
        File.WriteAllBytes(tempFile, data);

        Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
    }

    private void OnSetExportPathClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Ablageort für Rechnungen wählen"
        };

        var settings = AppSettingsService.Load();
        if (!string.IsNullOrWhiteSpace(settings.ExportPath))
        {
            dialog.InitialDirectory = settings.ExportPath;
        }

        if (dialog.ShowDialog(this) == true)
        {
            AppSettingsService.Save(settings with { ExportPath = dialog.FolderName });
            StatusText.Text = $"Ablageort festgelegt: {dialog.FolderName}";
        }
    }

    private void OnClassificationRulesClick(object sender, RoutedEventArgs e)
    {
        var window = new ClassificationRulesWindow { Owner = this };
        window.ShowDialog();
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e) => _view.Refresh();

    private void OnCancelClick(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void OnAccountsClick(object sender, RoutedEventArgs e)
    {
        var window = new AccountsWindow { Owner = this };
        window.ShowDialog();
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void SetSearchingState(bool isSearching)
    {
        SearchButton.IsEnabled = !isSearching;
        ReclassifyButton.IsEnabled = !isSearching && _results.Count > 0;
        ExportButton.IsEnabled = !isSearching && _results.Count > 0;
        CancelButton.IsEnabled = isSearching;
        SearchProgress.Visibility = isSearching ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateButtonStates()
    {
        ReclassifyButton.IsEnabled = _results.Count > 0;
        ExportButton.IsEnabled = _results.Count > 0;
    }

    private static string GetMonthDirectory(string? invoiceDate)
    {
        if (!string.IsNullOrWhiteSpace(invoiceDate) &&
            DateTime.TryParse(invoiceDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return s_germanMonths[date.Month - 1];
        }

        return "Unbekannt";
    }

    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path))
            return path;

        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var counter = 1;
        string candidate;

        do
        {
            candidate = Path.Combine(dir, $"{name} ({counter}){ext}");
            counter++;
        }
        while (File.Exists(candidate));

        return candidate;
    }
}