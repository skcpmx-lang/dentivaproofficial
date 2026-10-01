using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using DentivaPro.Application.Abstractions;

namespace DentivaPro.Infrastructure.Security;

/// <summary>Windows DPAPI CurrentUser protection for small local secrets.</summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
    private const int MaximumProtectedValueBytes = 64 * 1024;
    private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("DentivaPro.DatabaseKey.v1");

    public byte[] Protect(ReadOnlySpan<byte> plaintext)
    {
        if (plaintext.IsEmpty || plaintext.Length > MaximumProtectedValueBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(plaintext));
        }

        var copy = plaintext.ToArray();
        try
        {
            return ProtectedData.Protect(copy, OptionalEntropy, DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copy);
        }
    }

    public byte[] Unprotect(ReadOnlySpan<byte> protectedData)
    {
        if (protectedData.IsEmpty || protectedData.Length > MaximumProtectedValueBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(protectedData));
        }

        var copy = protectedData.ToArray();
        try
        {
            return ProtectedData.Unprotect(copy, OptionalEntropy, DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copy);
        }
    }
}
