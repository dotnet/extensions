// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.AI.Templates.Tests;

public sealed class MessageSinkTestOutputHelper : ITestOutputHelper
{
    private readonly IMessageSink _messageSink;

    public MessageSinkTestOutputHelper(IMessageSink messageSink)
    {
        _messageSink = messageSink;
    }

    public void WriteLine(string message)
    {
        _messageSink.OnMessage(new DiagnosticMessage(message));
    }

    public void WriteLine(string format, params object[] args)
    {
        _messageSink.OnMessage(new DiagnosticMessage(format, args));
    }
}
