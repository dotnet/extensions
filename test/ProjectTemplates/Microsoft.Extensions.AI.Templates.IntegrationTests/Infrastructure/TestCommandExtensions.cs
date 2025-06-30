// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Templates.Tests;

public static class TestCommandExtensions
{
    public static TCommand WithEnvironmentVariable<TCommand>(this TCommand command, string name, string value)
        where TCommand : TestCommand
    {
        command.EnvironmentVariables[name] = value;
        return command;
    }

    public static TCommand WithWorkingDirectory<TCommand>(this TCommand command, string workingDirectory)
        where TCommand : TestCommand
    {
        command.WorkingDirectory = workingDirectory;
        return command;
    }

    public static TCommand WithTimeout<TCommand>(this TCommand command, TimeSpan timeout)
        where TCommand : TestCommand
    {
        command.Timeout = timeout;
        return command;
    }
}
