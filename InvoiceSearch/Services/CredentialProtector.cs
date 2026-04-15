using System.Security.Cryptography;
using System.Text;

namespace InvoiceSearch.Services;

/// <summary>
/// Provides DPAPI-based encryption and decryption for credential storage.
/// </summary>
public static class CredentialProtector
{
    /// <summary>
    /// Encrypts a plain-text string using DPAPI (current user scope).
    /// </summary>
    public static byte[] Protect(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
    }

    /// <summary>
    /// Decrypts DPAPI-protected data back to a plain-text string.
    /// </summary>
    public static string Unprotect(byte[] encryptedData)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        var bytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
