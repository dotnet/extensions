// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public sealed class TestCommandResult(StringBuilder standardOutputBuilder, StringBuilder standardErrorBuilder, int exitCode)
{
    public string StandardOutput => field ??= standardOutputBuilder.ToString();

    public string StandardError => field ??= standardErrorBuilder.ToString();

    public int ExitCode => exitCode;
}
