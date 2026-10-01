namespace DentivaPro.Application.Security;

/// <summary>Versioned, encoded password verifier. Never contains a plaintext password.</summary>
public sealed record PasswordHash(string EncodedValue);
