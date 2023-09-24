// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging.Testing;

[ProviderAlias("Fake")]
public class FakeLoggerProvider : ILoggerProvider, IDisposable, ISupportExternalScope
{
    public FakeLogCollector Collector { get; }
    public FakeLoggerProvider(FakeLogCollector? collector = null);
    [ExcludeFromCodeCoverage]
    ~FakeLoggerProvider();
    public void SetScopeProvider(IExternalScopeProvider scopeProvider);
    public FakeLogger CreateLogger(string? categoryName);
    public void Dispose();
    protected virtual void Dispose(bool disposing);
}
