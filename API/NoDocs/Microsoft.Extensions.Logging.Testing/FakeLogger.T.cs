// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;

namespace Microsoft.Extensions.Logging.Testing;

public sealed class FakeLogger<T> : FakeLogger, ILogger<T>, ILogger
{
    public FakeLogger(FakeLogCollector? collector = null);
    public FakeLogger(Action<string> outputSink);
}
