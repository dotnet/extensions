// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging.Testing;

public class FakeLogRecord
{
    public LogLevel Level { get; }
    public EventId Id { get; }
    public object? State { get; }
    public IReadOnlyList<KeyValuePair<string, string?>>? StructuredState { get; }
    public Exception? Exception { get; }
    public string Message { get; }
    public IReadOnlyList<object?> Scopes { get; }
    public string? Category { get; }
    public bool LevelEnabled { get; }
    public DateTimeOffset Timestamp { get; }
    public FakeLogRecord(LogLevel level, EventId id, object? state, Exception? exception, string message, IReadOnlyList<object?> scopes, string? category, bool enabled, DateTimeOffset timestamp);
    public override string ToString();
}
