using System.Windows;
using System.Windows.Controls;
using InvoiceSearch.Models;

namespace InvoiceSearch;

/// <summary>
/// Dialog for creating or editing a classification rule.
/// </summary>
public partial class ClassificationRuleDialog : Window
{
    private readonly int _ruleId;

    /// <summary>
    /// The resulting rule after a successful save, or <c>null</c> if cancelled.
    /// </summary>
    public ClassificationRule? ResultRule { get; private set; }

    public ClassificationRuleDialog(ClassificationRule? existing = null)
    {
        InitializeComponent();

        if (existing is not null)
        {
            _ruleId = existing.Id;
            NameBox.Text = existing.Name;
            PatternBox.Text = existing.Pattern;

            foreach (ComboBoxItem item in ActionBox.Items)
            {
                if ((string)item.Tag == existing.Action)
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(PatternBox.Text))
        {
            MessageBox.Show(
                "Bitte Name und Muster ausfüllen.",
                "Validierung",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var action = (ActionBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Exclude";

        ResultRule = new ClassificationRule
        {
            Id = _ruleId,
            Name = NameBox.Text.Trim(),
            Pattern = PatternBox.Text.Trim(),
            Action = action
        };

        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
