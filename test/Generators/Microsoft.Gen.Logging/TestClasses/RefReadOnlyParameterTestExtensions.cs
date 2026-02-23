// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class RefReadOnlyParameterTestExtensions
    {
        internal struct S
        {
            public override readonly string ToString() => "Hello from S";
        }

        [LoggerMessage(0, LogLevel.Information, "M0 {s}")]
        internal static partial void M0(ILogger logger, ref readonly S s);

        [LoggerMessage(1, LogLevel.Information, "M1 {s}")]
        internal static partial void M1(ILogger logger, scoped ref readonly S s);
    }
}
