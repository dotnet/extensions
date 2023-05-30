// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

public sealed class FakeLogger<T> : FakeLogger, ILogger<T>, ILogger
{
    public FakeLogger(FakeLogCollector? collector = null);
    public FakeLogger(Action<string> outputSink);
}
