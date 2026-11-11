// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class GenericTestExtensions
    {
        // generic method with single type parameter
        [LoggerMessage(0, LogLevel.Debug, "M1 {value}")]
        internal static partial void M1<T>(ILogger logger, T value);

        // generic method with struct+Enum constraint
        [LoggerMessage(1, LogLevel.Debug, "M2 {code}")]
        internal static partial void M2<TCode>(ILogger logger, TCode code)
            where TCode : struct, Enum;

        // generic method with multiple type parameters
        [LoggerMessage(2, LogLevel.Debug, "M3 {p1} {p2}")]
        internal static partial void M3<T1, T2>(ILogger logger, T1 p1, T2 p2)
            where T1 : class
            where T2 : notnull;

        // generic method with new() constraint
        [LoggerMessage(3, LogLevel.Debug, "M4 {value}")]
        internal static partial void M4<T>(ILogger logger, T value)
            where T : new();
    }
}
