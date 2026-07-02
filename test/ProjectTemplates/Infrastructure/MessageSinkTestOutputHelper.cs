// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Sdk;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public sealed class MessageSinkTestOutputHelper : ITestOutputHelper
{
    private readonly IMessageSink _messageSink;

    public MessageSinkTestOutputHelper(IMessageSink messageSink)
    {
        _messageSink = messageSink;
    }

    public string Output { get; private set; } = string.Empty;

    public void Write(string message)
    {
        Output += message;
        _messageSink.OnMessage(new Xunit.v3.DiagnosticMessage(message));
    }

    public void Write(string format, params object[] args)
    {
        _messageSink.OnMessage(new Xunit.v3.DiagnosticMessage(format, args));
        Output += string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
    }

    public void WriteLine(string message)
    {
        Output += message + System.Environment.NewLine;
        _messageSink.OnMessage(new Xunit.v3.DiagnosticMessage(message));
    }

    public void WriteLine(string format, params object[] args)
    {
        Output += string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args) + System.Environment.NewLine;
        _messageSink.OnMessage(new Xunit.v3.DiagnosticMessage(format, args));
    }
}
