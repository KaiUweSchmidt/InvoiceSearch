using InvoiceSearch.Services;

namespace InvoiceSearch.Tests.Services;

public class CredentialProtectorTests
{
    [Fact]
    public void WhenProtectAndUnprotectThenOriginalTextIsReturned()
    {
        var plainText = "MySecretP@ssw0rd!";

        var encrypted = CredentialProtector.Protect(plainText);
        var decrypted = CredentialProtector.Unprotect(encrypted);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void WhenProtectCalledThenResultDiffersFromPlainText()
    {
        var plainText = "TestPassword";

        var encrypted = CredentialProtector.Protect(plainText);

        Assert.NotEqual(System.Text.Encoding.UTF8.GetBytes(plainText), encrypted);
    }

    [Fact]
    public void WhenProtectEmptyStringThenRoundtripSucceeds()
    {
        var plainText = string.Empty;

        var encrypted = CredentialProtector.Protect(plainText);
        var decrypted = CredentialProtector.Unprotect(encrypted);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void WhenProtectCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CredentialProtector.Protect(null!));
    }

    [Fact]
    public void WhenUnprotectCalledWithNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CredentialProtector.Unprotect(null!));
    }

    [Fact]
    public void WhenProtectCalledWithUnicodeTextThenRoundtripSucceeds()
    {
        var plainText = "Pässwörd123€";

        var encrypted = CredentialProtector.Protect(plainText);
        var decrypted = CredentialProtector.Unprotect(encrypted);

        Assert.Equal(plainText, decrypted);
    }
}
