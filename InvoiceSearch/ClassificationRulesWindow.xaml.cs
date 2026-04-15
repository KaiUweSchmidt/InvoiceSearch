using System.Windows;
using System.Windows.Input;
using InvoiceSearch.Data;
using InvoiceSearch.Models;

namespace InvoiceSearch;

/// <summary>
/// Window for managing classification rules (add, edit, delete).
/// </summary>
public partial class ClassificationRulesWindow : Window
{
    private readonly ClassificationRuleRepository _repository = new();

    public ClassificationRulesWindow()
    {
        InitializeComponent();
        RefreshList();
    }

    private void RefreshList() => RuleList.ItemsSource = _repository.GetAll();

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ClassificationRuleDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.ResultRule is not null)
        {
            _repository.Add(dialog.ResultRule);
            RefreshList();
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (RuleList.SelectedItem is not ClassificationRule selected)
        {
            MessageBox.Show(
                "Bitte wählen Sie eine Regel aus.",
                "Bearbeiten",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new ClassificationRuleDialog(selected) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.ResultRule is not null)
        {
            _repository.Update(dialog.ResultRule);
            RefreshList();
        }
    }

    private void OnEditDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RuleList.SelectedItem is ClassificationRule selected)
        {
            var dialog = new ClassificationRuleDialog(selected) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultRule is not null)
            {
                _repository.Update(dialog.ResultRule);
                RefreshList();
            }
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (RuleList.SelectedItem is not ClassificationRule selected)
        {
            MessageBox.Show(
                "Bitte wählen Sie eine Regel aus.",
                "Löschen",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Regel \"{selected.Name}\" wirklich löschen?",
            "Regel löschen",
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
