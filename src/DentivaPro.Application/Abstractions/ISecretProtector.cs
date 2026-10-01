namespace DentivaPro.Application.Abstractions;

/// <summary>Protects small secrets using the current Windows user's data-protection scope.</summary>
public interface ISecretProtector
{
    byte[] Protect(ReadOnlySpan<byte> plaintext);

    byte[] Unprotect(ReadOnlySpan<byte> protectedData);
}
