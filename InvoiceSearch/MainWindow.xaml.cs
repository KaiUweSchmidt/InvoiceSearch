using System.Collections.ObjectModel;
using System.Windows;
using InvoiceSearch.Data;
using InvoiceSearch.Models;
using InvoiceSearch.Services;

namespace InvoiceSearch;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<InvoiceSearchResult> _results = [];
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        ResultsGrid.ItemsSource = _results;
    }

    private async void OnSearchClick(object sender, RoutedEventArgs e)
    {
        List<EmailAccount> accounts;
        using (var repository = new AccountRepository())
        {
            accounts = repository.GetAll();
        }

        if (accounts.Count == 0)
        {
            MessageBox.Show(
                "Keine E-Mail-Konten konfiguriert.\nBitte zuerst unter Datei → Konten ein Konto anlegen.",
                "Keine Konten",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _results.Clear();
        _cts = new CancellationTokenSource();
        SetSearchingState(true);

        var progress = new Progress<string>(message => StatusText.Text = message);
        var searchService = new MailSearchService();
        var ollamaService = new OllamaService();

        try
        {
            foreach (var account in accounts)
            {
                await foreach (var result in searchService.SearchAccountAsync(account, ollamaService, progress, _cts.Token))
                {
                    _results.Add(result);
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
        CancelButton.IsEnabled = isSearching;
        SearchProgress.Visibility = isSearching ? Visibility.Visible : Visibility.Collapsed;
    }
}