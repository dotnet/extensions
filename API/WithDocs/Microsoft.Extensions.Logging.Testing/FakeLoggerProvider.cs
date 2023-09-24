// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging.Testing;

/// <summary>
/// Provides fake loggers.
/// </summary>
[ProviderAlias("Fake")]
public class FakeLoggerProvider : ILoggerProvider, IDisposable, ISupportExternalScope
{
    /// <summary>
    /// Gets the log record collector for all loggers created by this provider.
    /// </summary>
    public FakeLogCollector Collector { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLoggerProvider" /> class.
    /// </summary>
    /// <param name="collector">The collector that will receive all log records emitted to fake loggers.</param>
    public FakeLoggerProvider(FakeLogCollector? collector = null);

    /// <summary>
    /// Finalizes an instance of the <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLoggerProvider" /> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    ~FakeLoggerProvider();

    /// <summary>
    /// Sets external scope information source for logger provider.
    /// </summary>
    /// <param name="scopeProvider">The provider of scope data.</param>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider);

    /// <summary>
    /// Creates a new <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    public FakeLogger CreateLogger(string? categoryName);

    /// <summary>
    /// Clean up resources held by this object.
    /// </summary>
    public void Dispose();

    /// <summary>
    /// Clean up resources held by this object.
    /// </summary>
    /// <param name="disposing"><see langword="true" /> when called from the <see cref="M:Microsoft.Extensions.Logging.Testing.FakeLoggerProvider.Dispose" /> method, <see langword="false" /> when called from a finalizer.</param>
    protected virtual void Dispose(bool disposing);
}
