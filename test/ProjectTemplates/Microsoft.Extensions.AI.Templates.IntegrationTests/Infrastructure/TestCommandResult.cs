// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Extensions.AI.Templates.Tests;

public sealed class TestCommandResult(StringBuilder standardOutputBuilder, StringBuilder standardErrorBuilder, int exitCode)
{
    private string? _standardOutput;
    private string? _standardError;

    public string StandardOutput => _standardOutput ??= standardOutputBuilder.ToString();

    public string StandardError => _standardError ??= standardErrorBuilder.ToString();

    public int ExitCode => exitCode;
}
