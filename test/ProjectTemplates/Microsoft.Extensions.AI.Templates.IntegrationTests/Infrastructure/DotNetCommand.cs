// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Templates.Tests;

public class DotNetCommand : TestCommand
{
    public DotNetCommand(params ReadOnlySpan<string> args)
    {
        FileName = WellKnownPaths.RepoDotNetExePath;

        foreach (var arg in args)
        {
            Arguments.Add(arg);
        }
    }
}
