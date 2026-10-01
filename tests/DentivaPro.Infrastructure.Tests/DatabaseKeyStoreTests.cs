using System.Security.Cryptography;
using DentivaPro.Infrastructure.Security;

namespace DentivaPro.Infrastructure.Tests;

[TestClass]
public sealed class DatabaseKeyStoreTests
{
    private string _temporaryDirectory = string.Empty;

    [TestInitialize]
    public void SetUp() => _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"DentivaPro-tests-{Guid.NewGuid():N}");

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void GetOrCreate_RoundTripsProtectedKeyAndDoesNotWriteTheRawKey()
    {
        var path = Path.Combine(_temporaryDirectory, "Key", "database-key.dpapi");
        var databasePath = Path.Combine(_temporaryDirectory, "clinic.db3");
        var protector = new TestOnlySecretProtector();
        var store = new FileDatabaseKeyStore(path, databasePath, protector);

        var key = store.GetOrCreateDatabaseKey();
        var fileBytes = File.ReadAllBytes(path);
        var secondStoreKey = new FileDatabaseKeyStore(path, databasePath, protector).GetOrCreateDatabaseKey();

        Assert.AreEqual(FileDatabaseKeyStore.KeySizeBytes, key.Length);
        Assert.IsTrue(File.ReadAllText(path).StartsWith("DENTIVAPRO-DBKEY-V1", StringComparison.Ordinal));
        Assert.IsFalse(ContainsSequence(fileBytes, key));
        CollectionAssert.AreEqual(key, secondStoreKey);
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(secondStoreKey);
        CryptographicOperations.ZeroMemory(fileBytes);
    }

    [TestMethod]
    public void GetOrCreate_FailsClosedForCorruptExistingKeyFile()
    {
        var path = Path.Combine(_temporaryDirectory, "database-key.dpapi");
        var databasePath = Path.Combine(_temporaryDirectory, "clinic.db3");
        Directory.CreateDirectory(_temporaryDirectory);
        File.WriteAllText(path, "unsupported key envelope");
        var original = File.ReadAllBytes(path);
        var store = new FileDatabaseKeyStore(path, databasePath, new TestOnlySecretProtector());

        Assert.ThrowsExactly<CryptographicException>(() => store.GetOrCreateDatabaseKey());
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
    }

    [TestMethod]
    public void GetOrCreate_FailsClosedWhenDatabaseExistsButItsKeyFileIsMissing()
    {
        var path = Path.Combine(_temporaryDirectory, "database-key.dpapi");
        var databasePath = Path.Combine(_temporaryDirectory, "clinic.db3");
        Directory.CreateDirectory(_temporaryDirectory);
        File.WriteAllBytes(databasePath, [0x01, 0x02, 0x03]);
        var store = new FileDatabaseKeyStore(path, databasePath, new TestOnlySecretProtector());

        Assert.ThrowsExactly<CryptographicException>(() => store.GetOrCreateDatabaseKey());
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public void SeparateStoresRacingToCreateKey_ConvergeOnTheWinningProtectedKey()
    {
        var path = Path.Combine(_temporaryDirectory, "database-key.dpapi");
        var databasePath = Path.Combine(_temporaryDirectory, "clinic.db3");
        var protector = new TestOnlySecretProtector();
        var results = new byte[8][];

        Parallel.For(0, results.Length, index =>
        {
            results[index] = new FileDatabaseKeyStore(path, databasePath, protector).GetOrCreateDatabaseKey();
        });

        for (var index = 1; index < results.Length; index++)
        {
            CollectionAssert.AreEqual(results[0], results[index]);
        }

        foreach (var result in results)
        {
            CryptographicOperations.ZeroMemory(result);
        }
    }

    private static bool ContainsSequence(byte[] container, byte[] sequence)
    {
        for (var start = 0; start <= container.Length - sequence.Length; start++)
        {
            if (container.AsSpan(start, sequence.Length).SequenceEqual(sequence))
            {
                return true;
            }
        }

        return false;
    }
}
