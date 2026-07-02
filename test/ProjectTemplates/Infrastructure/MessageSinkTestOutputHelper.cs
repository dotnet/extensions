// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Sdk;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public sealed class MessageSinkTestOutputHelper : ITestOutputHelper
{
    private readonly IMessageSink _messageSink;
    private string _output = string.Empty;

    public MessageSinkTestOutputHelper(IMessageSink messageSink)
    {
        _messageSink = messageSink;
    }

    public string Output => _output;

    public void Write(string message)
    {
        _output += message;
        _messageSink.OnMessage(new DiagnosticMessage(message));
    }

    public void Write(string format, params object[] args)
    {
        _messageSink.OnMessage(new DiagnosticMessage(format, args));
        _output += string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
    }

    public void WriteLine(string message)
    {
        _output += message + Environment.NewLine;
        _messageSink.OnMessage(new DiagnosticMessage(message));
    }

    public void WriteLine(string format, params object[] args)
    {
        _output += string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args) + Environment.NewLine;
        _messageSink.OnMessage(new DiagnosticMessage(format, args));
    }
}
