using System.Security.Cryptography;
using DentivaPro.Infrastructure.Security;

namespace DentivaPro.Infrastructure.Tests;

[TestClass]
public sealed class DpapiSecretProtectorTests
{
    [TestMethod]
    public void CurrentUserDpapi_RoundTripsASecretOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("DPAPI CurrentUser is Windows-only.");
            return;
        }

        var plaintext = RandomNumberGenerator.GetBytes(32);
        var protector = new DpapiSecretProtector();
        var protectedData = protector.Protect(plaintext);
        var restored = protector.Unprotect(protectedData);

        CollectionAssert.AreEqual(plaintext, restored);
        Assert.IsFalse(plaintext.AsSpan().SequenceEqual(protectedData));
        CryptographicOperations.ZeroMemory(plaintext);
        CryptographicOperations.ZeroMemory(protectedData);
        CryptographicOperations.ZeroMemory(restored);
    }
}
