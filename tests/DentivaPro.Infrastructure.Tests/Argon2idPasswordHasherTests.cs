using System.Text;
using DentivaPro.Application.Security;
using DentivaPro.Infrastructure.Security;

namespace DentivaPro.Infrastructure.Tests;

[TestClass]
public sealed class Argon2idPasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [TestMethod]
    public void Hash_UsesVersionedArgon2idEncodingAndFreshSalt()
    {
        var first = _hasher.Hash("Strong-test-password-2026");
        var second = _hasher.Hash("Strong-test-password-2026");

        Assert.IsTrue(first.EncodedValue.StartsWith("argon2id$v=19$m=19456,t=2,p=1$", StringComparison.Ordinal));
        Assert.AreNotEqual(first.EncodedValue, second.EncodedValue);
        Assert.IsTrue(_hasher.Verify("Strong-test-password-2026", first));
    }

    [TestMethod]
    public void Verify_UsesUnicodeNormalizationAndRejectsWrongPassword()
    {
        var hash = _hasher.Hash("café-দাঁত-12345");

        Assert.IsTrue(_hasher.Verify("cafe\u0301-দাঁত-12345", hash));
        Assert.IsFalse(_hasher.Verify("different-দাঁত-12345", hash));
    }

    [TestMethod]
    public void Hash_RejectsEmptyAndOversizedPasswords()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _hasher.Hash(string.Empty));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _hasher.Hash(new string('a', 1025)));
    }

    [TestMethod]
    public void Verify_RejectsMalformedOrUnrecognizedParameterStringsWithoutDeriving()
    {
        Assert.IsFalse(_hasher.Verify("candidate", new PasswordHash("not-a-hash")));
        Assert.IsFalse(_hasher.Verify(
            "candidate",
            new PasswordHash("argon2id$v=19$m=999999999,t=99,p=99$AA$AA")));
        Assert.IsFalse(_hasher.Verify("candidate", new PasswordHash("argon2id$v=19$m=19456,t=2,p=1$AA!$AA")));
    }

    [TestMethod]
    public void Hash_IsValidUtf8AndDoesNotEncodePlaintext()
    {
        const string password = "ক্লিনিক-কী-98765";
        var hash = _hasher.Hash(password);
        var serializedBytes = Encoding.UTF8.GetBytes(hash.EncodedValue);

        Assert.IsFalse(Encoding.UTF8.GetString(serializedBytes).Contains(password, StringComparison.Ordinal));
        Assert.IsTrue(_hasher.Verify(password, hash));
    }
}
