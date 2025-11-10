// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public sealed class TestCommandResult(StringBuilder standardOutputBuilder, StringBuilder standardErrorBuilder, int exitCode)
{
    public string StandardOutput => field ??= standardOutputBuilder.ToString();

    public string StandardError => field ??= standardErrorBuilder.ToString();

    public int ExitCode => exitCode;

    public void AssertSucceeded(string testDescription)
    {
        var output = $"""
        {testDescription}

        {(ExitCode != 0 ? $"""
            Command failed with non-zero exit code: {ExitCode}

            """ : string.Empty)}
        {(!string.IsNullOrWhiteSpace(StandardOutput) ?
            $"""
                >> Standard Output:
                {StandardOutput}

                """ : string.Empty)}
        {(!string.IsNullOrWhiteSpace(StandardError) ?
            $"""
                >>> Standard Error:
                {StandardError}

                """ : string.Empty)}
        """;

        Assert.True(ExitCode == 0 && string.IsNullOrWhiteSpace(StandardError), output);
    }
}
