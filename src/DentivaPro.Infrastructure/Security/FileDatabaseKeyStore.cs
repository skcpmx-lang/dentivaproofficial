using System.Security.Cryptography;
using System.Text;
using DentivaPro.Application.Abstractions;

namespace DentivaPro.Infrastructure.Security;

/// <summary>
/// Persists only a DPAPI-protected random 256-bit key. Existing invalid key material fails closed;
/// it is never silently replaced because doing so would make an existing database unreadable.
/// </summary>
public sealed class FileDatabaseKeyStore : IDatabaseKeyStore
{
    public const int KeySizeBytes = 32;
    private static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("DENTIVAPRO-DBKEY-V1\n");

    private readonly string _keyPath;
    private readonly string _databasePath;
    private readonly ISecretProtector _secretProtector;
    private readonly object _gate = new();

    public FileDatabaseKeyStore(string keyPath, string databasePath, ISecretProtector secretProtector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(secretProtector);
        _keyPath = Path.GetFullPath(keyPath);
        _databasePath = Path.GetFullPath(databasePath);
        _secretProtector = secretProtector;
    }

    public byte[] GetOrCreateDatabaseKey()
    {
        lock (_gate)
        {
            var directory = Path.GetDirectoryName(_keyPath)
                ?? throw new InvalidOperationException("The database key path has no parent directory.");
            Directory.CreateDirectory(directory);

            if (File.Exists(_keyPath))
            {
                return LoadExistingKey();
            }

            if (DatabaseFilesExist())
            {
                throw new CryptographicException("The database key is missing for an existing database; refusing to replace it.");
            }

            return CreateAndPersistKey(directory);
        }
    }

    private bool DatabaseFilesExist() =>
        File.Exists(_databasePath) ||
        File.Exists(string.Concat(_databasePath, "-wal")) ||
        File.Exists(string.Concat(_databasePath, "-shm")) ||
        File.Exists(string.Concat(_databasePath, "-journal"));

    private byte[] CreateAndPersistKey(string directory)
    {
        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        byte[]? protectedKey = null;
        var temporaryPath = Path.Combine(directory, $".database-key-{Guid.NewGuid():N}.tmp");
        var keyTransferredToCaller = false;

        try
        {
            protectedKey = _secretProtector.Protect(key);
            if (protectedKey.Length == 0 || protectedKey.Length > 64 * 1024)
            {
                throw new CryptographicException("The protected database key has an invalid size.");
            }

            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                stream.Write(FileHeader);
                stream.Write(protectedKey);
                stream.Flush(flushToDisk: true);
            }

            try
            {
                File.Move(temporaryPath, _keyPath, overwrite: false);
                keyTransferredToCaller = true;
                return key;
            }
            catch (IOException) when (File.Exists(_keyPath))
            {
                return LoadExistingKey();
            }
        }
        finally
        {
            if (!keyTransferredToCaller)
            {
                CryptographicOperations.ZeroMemory(key);
            }

            if (protectedKey is not null)
            {
                CryptographicOperations.ZeroMemory(protectedKey);
            }

            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                // A leftover temporary file contains only DPAPI-protected bytes.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve the original operation result; no plaintext key is written there.
            }
        }
    }

    private byte[] LoadExistingKey()
    {
        byte[] envelope;
        using (var stream = new FileStream(_keyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length <= FileHeader.Length || stream.Length > FileHeader.Length + 64 * 1024)
            {
                throw new CryptographicException("The stored database key is invalid or unsupported.");
            }

            envelope = new byte[checked((int)stream.Length)];
            stream.ReadExactly(envelope);
        }

        byte[]? protectedKey = null;
        byte[]? plaintextKey = null;

        try
        {
            if (!CryptographicOperations.FixedTimeEquals(envelope.AsSpan(0, FileHeader.Length), FileHeader))
            {
                throw new CryptographicException("The stored database key is invalid or unsupported.");
            }

            protectedKey = envelope.AsSpan(FileHeader.Length).ToArray();
            plaintextKey = _secretProtector.Unprotect(protectedKey);
            if (plaintextKey.Length != KeySizeBytes)
            {
                throw new CryptographicException("The stored database key has an invalid size.");
            }

            var result = plaintextKey;
            plaintextKey = null;
            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(envelope);
            if (protectedKey is not null)
            {
                CryptographicOperations.ZeroMemory(protectedKey);
            }

            if (plaintextKey is not null)
            {
                CryptographicOperations.ZeroMemory(plaintextKey);
            }
        }
    }
}
