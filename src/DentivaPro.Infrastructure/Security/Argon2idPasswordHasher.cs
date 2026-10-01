using System.Security.Cryptography;
using System.Text;
using DentivaPro.Application.Security;
using NSec.Cryptography;

namespace DentivaPro.Infrastructure.Security;

/// <summary>
/// Argon2id v=19 with OWASP's 19 MiB / 2-pass / 1-lane minimum baseline.
/// Re-benchmark and version parameters before production use; callers still own password policy and rate limiting.
/// </summary>
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const long ArgonMemorySizeKiB = 19 * 1024;
    private const long ArgonNumberOfPasses = 2;
    private const int ArgonDegreeOfParallelism = 1;
    private const int MaximumPasswordUtf8Bytes = 1024;
    private const string EncodedParameters = "m=19456,t=2,p=1";

    private static readonly Argon2Parameters Parameters = new()
    {
        DegreeOfParallelism = ArgonDegreeOfParallelism,
        MemorySize = ArgonMemorySizeKiB,
        NumberOfPasses = ArgonNumberOfPasses
    };

    public PasswordHash Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var passwordBytes = GetPasswordBytes(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[]? derivedKey = null;

        try
        {
            derivedKey = PasswordBasedKeyDerivationAlgorithm.Argon2id(Parameters)
                .DeriveBytes(passwordBytes, salt, HashSizeBytes);

            var encoded = string.Join(
                '$',
                "argon2id",
                "v=19",
                EncodedParameters,
                EncodeBase64Url(salt),
                EncodeBase64Url(derivedKey));

            return new PasswordHash(encoded);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(salt);
            if (derivedKey is not null)
            {
                CryptographicOperations.ZeroMemory(derivedKey);
            }
        }
    }

    public bool Verify(string password, PasswordHash passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (!TryParse(passwordHash.EncodedValue, out var salt, out var expectedHash))
        {
            return false;
        }

        byte[]? passwordBytes = null;
        byte[]? derivedKey = null;
        try
        {
            passwordBytes = GetPasswordBytes(password);
            derivedKey = PasswordBasedKeyDerivationAlgorithm.Argon2id(Parameters)
                .DeriveBytes(passwordBytes, salt, HashSizeBytes);

            return CryptographicOperations.FixedTimeEquals(derivedKey, expectedHash);
        }
        catch (ArgumentException)
        {
            // Malformed Unicode or an oversized candidate is an ordinary verification failure.
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expectedHash);
            if (passwordBytes is not null)
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }

            if (derivedKey is not null)
            {
                CryptographicOperations.ZeroMemory(derivedKey);
            }
        }
    }

    private static byte[] GetPasswordBytes(string password)
    {
        if (password.Length == 0)
        {
            throw new ArgumentException("A password cannot be empty.", nameof(password));
        }

        var normalized = password.Normalize(NormalizationForm.FormC);
        if (Encoding.UTF8.GetByteCount(normalized) > MaximumPasswordUtf8Bytes)
        {
            throw new ArgumentOutOfRangeException(nameof(password), "The password exceeds the supported input size.");
        }

        return Encoding.UTF8.GetBytes(normalized);
    }

    private static bool TryParse(string? encoded, out byte[] salt, out byte[] hash)
    {
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(encoded) || encoded.Length > 256)
        {
            return false;
        }

        var segments = encoded.Split('$');
        if (segments.Length != 5 ||
            !string.Equals(segments[0], "argon2id", StringComparison.Ordinal) ||
            !string.Equals(segments[1], "v=19", StringComparison.Ordinal) ||
            !string.Equals(segments[2], EncodedParameters, StringComparison.Ordinal) ||
            !TryDecodeBase64Url(segments[3], out var parsedSalt) ||
            !TryDecodeBase64Url(segments[4], out var parsedHash))
        {
            return false;
        }

        if (parsedSalt.Length != SaltSizeBytes || parsedHash.Length != HashSizeBytes)
        {
            CryptographicOperations.ZeroMemory(parsedSalt);
            CryptographicOperations.ZeroMemory(parsedHash);
            return false;
        }

        salt = parsedSalt;
        hash = parsedHash;
        return true;
    }

    private static string EncodeBase64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryDecodeBase64Url(string input, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (input.Length == 0 || input.Length % 4 == 1)
        {
            return false;
        }

        foreach (var character in input)
        {
            if (!(character is >= 'A' and <= 'Z') &&
                !(character is >= 'a' and <= 'z') &&
                !(character is >= '0' and <= '9') &&
                character is not '-' and not '_')
            {
                return false;
            }
        }

        var base64 = input.Replace('-', '+').Replace('_', '/');
        base64 = string.Concat(base64, new string('=', (4 - (base64.Length % 4)) % 4));

        try
        {
            bytes = Convert.FromBase64String(base64);
            if (!string.Equals(EncodeBase64Url(bytes), input, StringComparison.Ordinal))
            {
                CryptographicOperations.ZeroMemory(bytes);
                bytes = Array.Empty<byte>();
                return false;
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
