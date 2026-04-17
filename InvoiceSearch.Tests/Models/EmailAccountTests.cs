using InvoiceSearch.Models;

namespace InvoiceSearch.Tests.Models;

public class EmailAccountTests
{
    [Fact]
    public void WhenCreatedWithDefaultsThenImapPortIs993()
    {
        var account = new EmailAccount();

        Assert.Equal(993, account.ImapPort);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenUseSslIsTrue()
    {
        var account = new EmailAccount();

        Assert.True(account.UseSsl);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenEncryptedPasswordIsEmpty()
    {
        var account = new EmailAccount();

        Assert.Empty(account.EncryptedPassword);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenStringPropertiesAreEmpty()
    {
        var account = new EmailAccount();

        Assert.Equal(string.Empty, account.DisplayName);
        Assert.Equal(string.Empty, account.EmailAddress);
        Assert.Equal(string.Empty, account.ImapServer);
        Assert.Equal(string.Empty, account.Username);
    }

    [Fact]
    public void WhenCreatedWithValuesThenPropertiesAreSet()
    {
        var account = new EmailAccount
        {
            Id = 1,
            DisplayName = "Work",
            EmailAddress = "user@example.com",
            ImapServer = "imap.example.com",
            ImapPort = 143,
            UseSsl = false,
            Username = "user",
            EncryptedPassword = [0x01, 0x02]
        };

        Assert.Equal(1, account.Id);
        Assert.Equal("Work", account.DisplayName);
        Assert.Equal("user@example.com", account.EmailAddress);
        Assert.Equal("imap.example.com", account.ImapServer);
        Assert.Equal(143, account.ImapPort);
        Assert.False(account.UseSsl);
        Assert.Equal("user", account.Username);
        Assert.Equal(new byte[] { 0x01, 0x02 }, account.EncryptedPassword);
    }
}
