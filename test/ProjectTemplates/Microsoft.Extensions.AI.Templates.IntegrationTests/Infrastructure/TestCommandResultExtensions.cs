// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI.Templates.Tests;

public static class TestCommandResultExtensions
{
    public static TestCommandResult AssertZeroExitCode(this TestCommandResult result)
    {
        Assert.True(result.ExitCode == 0, $"Expected an exit code of zero, got {result.ExitCode}");
        return result;
    }

    public static TestCommandResult AssertEmptyStandardError(this TestCommandResult result)
    {
        var standardError = result.StandardError;
        Assert.True(string.IsNullOrWhiteSpace(standardError), $"Standard error output was unexpectedly non-empty:\n{standardError}");
        return result;
    }

    public static TestCommandResult AssertSucceeded(this TestCommandResult result)
        => result
            .AssertZeroExitCode()
            .AssertEmptyStandardError();
}
