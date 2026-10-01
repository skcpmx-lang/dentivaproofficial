using System.Security.Cryptography;
using DentivaPro.Application.Abstractions;

namespace DentivaPro.Infrastructure.Tests;

/// <summary>Test-only reversible transform; never registered by or shipped with the application.</summary>
internal sealed class TestOnlySecretProtector : ISecretProtector
{
    private const byte TestMarker = 0xA7;

    public byte[] Protect(ReadOnlySpan<byte> plaintext)
    {
        var result = new byte[plaintext.Length + 1];
        result[0] = TestMarker;
        for (var index = 0; index < plaintext.Length; index++)
        {
            result[index + 1] = (byte)(plaintext[index] ^ 0x5A);
        }

        return result;
    }

    public byte[] Unprotect(ReadOnlySpan<byte> protectedData)
    {
        if (protectedData.Length < 2 || protectedData[0] != TestMarker)
        {
            throw new CryptographicException("Invalid test-only protected value.");
        }

        var result = new byte[protectedData.Length - 1];
        for (var index = 1; index < protectedData.Length; index++)
        {
            result[index - 1] = (byte)(protectedData[index] ^ 0x5A);
        }

        return result;
    }
}
