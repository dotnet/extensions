// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging.Testing;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// A logger which captures everything logged to it and enables inspection.
/// </summary>
/// <remarks>
/// This type is intended for use in unit tests. It captures all the log state to memory and lets you inspect it
/// to validate that your code is logging what it should.
/// </remarks>
/// <typeparam name="T">The type whose name to use as a logger category.</typeparam>
#pragma warning disable CS8633
#pragma warning disable CS8766
public sealed class FakeLogger<T> : FakeLogger, ILogger<T>
#pragma warning restore CS8633
#pragma warning restore CS8766
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogger{T}"/> class.
    /// </summary>
    /// <param name="collector">Where to push all log state.</param>
    public FakeLogger(FakeLogCollector? collector = null)
        : base(collector, GetNiceNameOfT())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogger{T}"/> class that copies all log records to the given output sink.
    /// </summary>
    /// <param name="outputSink">Where to emit individual log records.</param>
    public FakeLogger(Action<string> outputSink)
        : this(FakeLogCollector.Create(new FakeLogCollectorOptions { OutputSink = outputSink }))
    {
    }

    private static string GetNiceNameOfT()
    {
        // we do all this stuff just to get the nice generated name for "T" that LoggerFactory takes care of.
        using var provider = new FakeLoggerProvider();
        using var factory = new LoggerFactory();
        factory.AddProvider(provider);
        _ = factory.CreateLogger<T>();
        return provider.FirstLoggerName;
    }
}
