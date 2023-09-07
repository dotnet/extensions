// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

/// <summary>
/// A logger which captures everything logged to it and enables inspection.
/// </summary>
/// <remarks>
/// This type is intended for use in unit tests. It captures all the log state to memory and lets you inspect it
/// to validate that your code is logging what it should.
/// </remarks>
/// <typeparam name="T">The type whose name to use as a logger category.</typeparam>
public sealed class FakeLogger<T> : FakeLogger, ILogger<T>, ILogger
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Testing.Logging.FakeLogger`1" /> class.
    /// </summary>
    /// <param name="collector">Where to push all log state.</param>
    public FakeLogger(FakeLogCollector? collector = null);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Testing.Logging.FakeLogger`1" /> class that copies all log records to the given output sink.
    /// </summary>
    /// <param name="outputSink">Where to emit individual log records.</param>
    public FakeLogger(Action<string> outputSink);
}
