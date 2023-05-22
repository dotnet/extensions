// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;

namespace Microsoft.Extensions.Telemetry.Console.Test.Helpers;

/// <summary>
/// To test exception with a stack trace.
/// </summary>
public class TestException : Exception
{
    public TestException(string message, string stackTrace)
        : base(message)
    {
        StackTrace = stackTrace;
    }

    public override string StackTrace { get; }
}
#endif
