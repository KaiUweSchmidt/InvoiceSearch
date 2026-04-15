using System.Windows;
using InvoiceSearch.Models;
using InvoiceSearch.Services;

namespace InvoiceSearch;

/// <summary>
/// Dialog for adding or editing an email account with mandatory IMAP connection test.
/// </summary>
public partial class AccountDialog : Window
{
    private readonly int _accountId;

    /// <summary>
    /// The resulting account after a successful save, or <c>null</c> if cancelled.
    /// </summary>
    public EmailAccount? ResultAccount { get; private set; }

    public AccountDialog(EmailAccount? existing = null)
    {
        InitializeComponent();

        if (existing is not null)
        {
            _accountId = existing.Id;
            DisplayNameBox.Text = existing.DisplayName;
            EmailAddressBox.Text = existing.EmailAddress;
            ImapServerBox.Text = existing.ImapServer;
            ImapPortBox.Text = existing.ImapPort.ToString();
            UseSslBox.IsChecked = existing.UseSsl;
            UsernameBox.Text = existing.Username;
            PasswordBox.Password = CredentialProtector.Unprotect(existing.EncryptedPassword);
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ImapServerBox.Text) ||
            string.IsNullOrWhiteSpace(UsernameBox.Text) ||
            string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            MessageBox.Show(
                "Bitte füllen Sie alle Pflichtfelder aus (IMAP-Server, Benutzername, Passwort).",
                "Validierung",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(ImapPortBox.Text, out var port) || port is < 1 or > 65535)
        {
            MessageBox.Show(
                "Bitte geben Sie einen gültigen Port ein (1–65535).",
                "Validierung",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var useSsl = UseSslBox.IsChecked == true;
        var server = ImapServerBox.Text.Trim();
        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;

        IsEnabled = false;
        try
        {
            await ImapTestService.TestConnectionAsync(server, port, useSsl, username, password);

            ResultAccount = new EmailAccount
            {
                Id = _accountId,
                DisplayName = DisplayNameBox.Text.Trim(),
                EmailAddress = EmailAddressBox.Text.Trim(),
                ImapServer = server,
                ImapPort = port,
                UseSsl = useSsl,
                Username = username,
                EncryptedPassword = CredentialProtector.Protect(password)
            };

            MessageBox.Show(
                "Verbindung erfolgreich!",
                "Login-Test",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Verbindung fehlgeschlagen:\n{ex.Message}",
                "Login-Test",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
