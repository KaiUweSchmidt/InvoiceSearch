using System.Windows;
using System.Windows.Input;
using InvoiceSearch.Data;
using InvoiceSearch.Models;

namespace InvoiceSearch;

/// <summary>
/// Window for managing email accounts (add, edit, delete).
/// </summary>
public partial class AccountsWindow : Window
{
    private readonly AccountRepository _repository = new();

    public AccountsWindow()
    {
        InitializeComponent();
        RefreshList();
    }

    private void RefreshList() => AccountList.ItemsSource = _repository.GetAll();

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AccountDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.ResultAccount is not null)
        {
            _repository.Add(dialog.ResultAccount);
            RefreshList();
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (AccountList.SelectedItem is not EmailAccount selected)
        {
            MessageBox.Show(
                "Bitte wählen Sie ein Konto aus.",
                "Bearbeiten",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new AccountDialog(selected) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.ResultAccount is not null)
        {
            _repository.Update(dialog.ResultAccount);
            RefreshList();
        }
    }

    private void OnEditClick(object sender, MouseButtonEventArgs e)
    {
        if (AccountList.SelectedItem is EmailAccount selected)
        {
            var dialog = new AccountDialog(selected) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultAccount is not null)
            {
                _repository.Update(dialog.ResultAccount);
                RefreshList();
            }
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (AccountList.SelectedItem is not EmailAccount selected)
        {
            MessageBox.Show(
                "Bitte wählen Sie ein Konto aus.",
                "Löschen",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Konto \"{selected.DisplayName}\" wirklich löschen?",
            "Konto löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _repository.Delete(selected.Id);
            RefreshList();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _repository.Dispose();
        base.OnClosed(e);
    }
}
