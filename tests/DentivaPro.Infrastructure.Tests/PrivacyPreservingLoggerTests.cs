using DentivaPro.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace DentivaPro.Infrastructure.Tests;

[TestClass]
public sealed class PrivacyPreservingLoggerTests
{
    private string _temporaryDirectory = string.Empty;

    [TestInitialize]
    public void SetUp() => _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"DentivaPro-log-tests-{Guid.NewGuid():N}");

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void StructuredLog_OmitsUnknownValuesAndExceptionMessages()
    {
        using (var provider = new PrivacyPreservingJsonFileLoggerProvider(_temporaryDirectory, 4096, 7))
        {
            var logger = provider.CreateLogger("DentivaPro.Authentication");
            logger.LogWarning(
                new EventId(42, "AuthenticationRejected"),
                "Rejected local sign-in for {PatientName}; code {ErrorCode}",
                "Sensitive Bengali patient name: রিমা",
                "ERR_AUTH_DENIED");
            logger.LogError(
                new EventId(43, "DatabaseFailure"),
                new InvalidOperationException("Sensitive record value: patient phone 01700000000"),
                "Database operation failed with {OperationCode}",
                "DB_QUERY_FAILED");
        }

        var path = Directory.EnumerateFiles(_temporaryDirectory, "*.jsonl").Single();
        var logText = File.ReadAllText(path);

        Assert.IsTrue(logText.Contains("ERR_AUTH_DENIED", StringComparison.Ordinal));
        Assert.IsTrue(logText.Contains("DB_QUERY_FAILED", StringComparison.Ordinal));
        Assert.IsTrue(logText.Contains("InvalidOperationException", StringComparison.Ordinal));
        Assert.IsFalse(logText.Contains("রিমা", StringComparison.Ordinal));
        Assert.IsFalse(logText.Contains("Sensitive record value", StringComparison.Ordinal));
        Assert.IsFalse(logText.Contains("01700000000", StringComparison.Ordinal));
    }

    [TestMethod]
    public void FreeFormAndInterpolatedMessages_AreNotWritten()
    {
        using (var provider = new PrivacyPreservingJsonFileLoggerProvider(_temporaryDirectory, 4096, 7))
        {
            var logger = provider.CreateLogger("DentivaPro.Foundation");
            logger.LogInformation("Do not persist a free-form sensitive value: {0}", "patient-name-value");
            var confidentialValue = "patient-name-value";
            logger.LogInformation($"Interpolated confidential detail {confidentialValue}");
        }

        var path = Directory.EnumerateFiles(_temporaryDirectory, "*.jsonl").Single();
        var logText = File.ReadAllText(path);

        Assert.IsFalse(logText.Contains("patient-name-value", StringComparison.Ordinal));
        Assert.IsTrue(logText.Contains("unstructured message omitted", StringComparison.Ordinal));
    }
}
