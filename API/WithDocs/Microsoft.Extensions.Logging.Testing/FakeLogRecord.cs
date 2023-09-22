// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging.Testing;

/// <summary>
/// A single log record tracked by <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogger" />.
/// </summary>
public class FakeLogRecord
{
    /// <summary>
    /// Gets the level used when producing the log record.
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    /// Gets the id representing the specific log statement.
    /// </summary>
    public EventId Id { get; }

    /// <summary>
    /// Gets the opaque state supplied by the caller when creating the log record.
    /// </summary>
    public object? State { get; }

    /// <summary>
    /// Gets the opaque state supplied by the caller when creating the log record as a read-only list.
    /// </summary>
    /// <remarks>
    /// When logging using the code generator logging model, the arguments you supply to the logging method are packaged into a single state object which is delivered to the <see cref="M:Microsoft.Extensions.Logging.ILogger.Log``1(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,``0,System.Exception,System.Func{``0,System.Exception,System.String})" />
    /// method. This state can be retrieved as a set of name/value pairs encoded in a read-only list.
    ///
    /// The object returned by this property is the same as what <see cref="P:Microsoft.Extensions.Logging.Testing.FakeLogRecord.State" /> returns, except it has been cast to a read-only list.
    /// </remarks>
    /// <exception cref="T:System.InvalidCastException">The state object is not compatible with supported logging model and is not a read-only list.</exception>
    public IReadOnlyList<KeyValuePair<string, string?>>? StructuredState { get; }

    /// <summary>
    /// Gets an optional exception associated with the log record.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the formatted message text for the record.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the logging scopes active when the log record was created.
    /// </summary>
    public IReadOnlyList<object?> Scopes { get; }

    /// <summary>
    /// Gets the optional category of this record.
    /// </summary>
    /// <remarks>
    /// The category corresponds to the T value in <see cref="T:Microsoft.Extensions.Logging.ILogger`1" />.
    /// </remarks>
    public string? Category { get; }

    /// <summary>
    /// Gets a value indicating whether the log level was enabled or disabled when this record was collected.
    /// </summary>
    public bool LevelEnabled { get; }

    /// <summary>
    /// Gets the time at which the log record was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogRecord" /> class.
    /// </summary>
    /// <param name="level">The level used when producing the log record.</param>
    /// <param name="id">The id representing the specific log statement.</param>
    /// <param name="state">The opaque state supplied by the caller when creating the log record.</param>
    /// <param name="exception">An optional exception associated with the log record.</param>
    /// <param name="message">The formatted message text for the record.</param>
    /// <param name="scopes">List of active scopes active for this log record.</param>
    /// <param name="category">The optional category for this record, which corresponds to the T in <see cref="T:Microsoft.Extensions.Logging.ILogger`1" />.</param>
    /// <param name="enabled">Whether the log level was enabled or not when the <see cref="M:Microsoft.Extensions.Logging.Testing.FakeLogger.Log``1(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,``0,System.Exception,System.Func{``0,System.Exception,System.String})" /> method was called.</param>
    /// <param name="timestamp">The time at which the log record was created.</param>
    public FakeLogRecord(LogLevel level, EventId id, object? state, Exception? exception, string message, IReadOnlyList<object?> scopes, string? category, bool enabled, DateTimeOffset timestamp);

    /// <summary>
    /// Returns a string representing this object.
    /// </summary>
    /// <returns>A string that helps identity this object.</returns>
    public override string ToString();
}
