using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DentivaPro.Infrastructure.Logging;

/// <summary>
/// A small local JSONL sink that records static templates and a strict allowlist of operational scalars.
/// It deliberately omits arbitrary log values, scopes, exception messages, and stack traces.
/// </summary>
public sealed class PrivacyPreservingJsonFileLoggerProvider : ILoggerProvider
{
    private static readonly HashSet<string> SafeNumericProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Count", "RecordCount", "DurationMs", "RetryCount", "StatusCode", "ExitCode"
    };

    private static readonly HashSet<string> SafeOperationalCodes = new(StringComparer.Ordinal)
    {
        "ERR_AUTH_DENIED",
        "DB_QUERY_FAILED",
        "DB_MIGRATION_FAILED",
        "DB_INTEGRITY_FAILED",
        "SEC_KEY_PROTECTION_FAILED",
        "APP_STARTUP_FAILED",
        "APP_SHUTDOWN_FAILED",
        "OP_STARTUP",
        "OP_SHUTDOWN"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _directory;
    private readonly long _maximumFileBytes;
    private readonly TimeSpan _retention;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private readonly object _gate = new();
    private FileStream? _stream;
    private string? _openDate;
    private int _partNumber;
    private volatile bool _disposed;

    public PrivacyPreservingJsonFileLoggerProvider(string directory, long maximumFileBytes, int retentionDays)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (maximumFileBytes is < 4096 or > 100 * 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFileBytes));
        }

        if (retentionDays is < 1 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays));
        }

        _directory = Path.GetFullPath(directory);
        _maximumFileBytes = maximumFileBytes;
        _retention = TimeSpan.FromDays(retentionDays);
        Directory.CreateDirectory(_directory);
        RemoveExpiredFiles(DateTimeOffset.UtcNow);
    }

    public ILogger CreateLogger(string categoryName) => new PrivacyPreservingLogger(this, categoryName);

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stream?.Dispose();
            _stream = null;
        }
    }

    private bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && !_disposed;

    private void Write<TState>(
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception)
    {
        var template = "<unstructured message omitted>";
        var safeProperties = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        var redactedFieldCount = 0;

        if (state is IEnumerable<KeyValuePair<string, object?>> fields)
        {
            foreach (var field in fields)
            {
                if (string.Equals(field.Key, "{OriginalFormat}", StringComparison.Ordinal))
                {
                    template = string.IsNullOrWhiteSpace(Convert.ToString(field.Value, CultureInfo.InvariantCulture))
                        ? "<unstructured message omitted>"
                        : Convert.ToString(field.Value, CultureInfo.InvariantCulture)!;
                    continue;
                }

                if (TryGetSafeProperty(field.Key, field.Value, out var safeValue))
                {
                    safeProperties[field.Key] = safeValue;
                }
                else
                {
                    redactedFieldCount++;
                }
            }
        }
        else if (state is not null)
        {
            redactedFieldCount++;
        }

        var entry = new LogEntry(
            DateTimeOffset.UtcNow,
            logLevel.ToString(),
            categoryName,
            eventId.Id,
            template,
            safeProperties,
            redactedFieldCount,
            exception?.GetType().Name);

        var json = JsonSerializer.SerializeToUtf8Bytes(entry, JsonOptions);
        var line = new byte[json.Length + 1];
        json.CopyTo(line, 0);
        line[^1] = (byte)'\n';
        if (line.Length > _maximumFileBytes)
        {
            line = "{\"event\":\"oversized privacy-filtered log entry omitted\"}\n"u8.ToArray();
        }

        AppendLine(line);
    }

    private static bool TryGetSafeProperty(string name, object? value, out object? safeValue)
    {
        safeValue = null;
        if (value is null)
        {
            return false;
        }

        if (SafeNumericProperties.Contains(name))
        {
            switch (value)
            {
                case byte or sbyte or short or ushort or int or uint or long or ulong or decimal:
                    safeValue = value;
                    return true;
                case float number when float.IsFinite(number):
                    safeValue = number;
                    return true;
                case double number when double.IsFinite(number):
                    safeValue = number;
                    return true;
                default:
                    return false;
            }
        }

        if (string.Equals(name, "CorrelationId", StringComparison.OrdinalIgnoreCase) && value is Guid correlationId)
        {
            safeValue = correlationId;
            return true;
        }

        if ((string.Equals(name, "ErrorCode", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(name, "OperationCode", StringComparison.OrdinalIgnoreCase)) &&
            value is string code && SafeOperationalCodes.Contains(code))
        {
            safeValue = code;
            return true;
        }

        return false;
    }

    private void AppendLine(byte[] line)
    {
        try
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                var currentDate = DateTimeOffset.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                if (!string.Equals(_openDate, currentDate, StringComparison.Ordinal))
                {
                    _stream?.Dispose();
                    _stream = null;
                    _openDate = currentDate;
                    _partNumber = 0;
                }

                EnsureWritableStream(currentDate, line.Length);
                _stream!.Write(line);
                _stream.Flush(flushToDisk: false);
            }
        }
        catch (IOException)
        {
            ReportWriteFailure();
        }
        catch (UnauthorizedAccessException)
        {
            ReportWriteFailure();
        }
        catch (ObjectDisposedException)
        {
            ReportWriteFailure();
        }
    }

    private static void ReportWriteFailure() =>
        System.Diagnostics.Trace.WriteLine("Dentiva Pro could not write one privacy-filtered log entry.");

    private void EnsureWritableStream(string date, int incomingBytes)
    {
        while (true)
        {
            var path = GetPartPath(date, _partNumber);
            if (_stream is null)
            {
                var currentSize = File.Exists(path) ? new FileInfo(path).Length : 0;
                if (currentSize + incomingBytes > _maximumFileBytes)
                {
                    _partNumber++;
                    continue;
                }

                _stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            }

            if (_stream.Length + incomingBytes <= _maximumFileBytes)
            {
                return;
            }

            _stream.Dispose();
            _stream = null;
            _partNumber++;
        }
    }

    private string GetPartPath(string date, int partNumber)
    {
        var part = partNumber == 0 ? string.Empty : $"-{partNumber:D3}";
        return Path.Combine(_directory, $"dentivapro-{date}-{_instanceId}{part}.jsonl");
    }

    private void RemoveExpiredFiles(DateTimeOffset now)
    {
        foreach (var path in Directory.EnumerateFiles(_directory, "dentivapro-*.jsonl", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var lastWrite = new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
                if (now - lastWrite > _retention)
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // A stale log in use by another process is retained for the next cleanup pass.
            }
            catch (UnauthorizedAccessException)
            {
                // Cleanup is best-effort; logging must not delete or overwrite an inaccessible file.
            }
        }
    }

    private sealed record LogEntry(
        DateTimeOffset TimestampUtc,
        string Level,
        string Category,
        int EventId,
        string MessageTemplate,
        IReadOnlyDictionary<string, object?> Properties,
        int RedactedFieldCount,
        string? ExceptionType);

    private sealed class PrivacyPreservingLogger(PrivacyPreservingJsonFileLoggerProvider provider, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                provider.Write(categoryName, logLevel, eventId, state, exception);
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
