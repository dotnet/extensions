// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

public class MockedLogger<T> : ILogger<T>
{
    public Mock<ILogger<T>> Mock { get; }

    public MockedLogger(Mock<ILogger<T>> mock)
    {
        Mock = mock;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Mock.Object.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => Mock.Object.IsEnabled(logLevel);

#pragma warning disable CS8633
#pragma warning disable CS8766
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => Mock.Object.BeginScope(state);
#pragma warning restore CS8633
#pragma warning restore CS8766
}
