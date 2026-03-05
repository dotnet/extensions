// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class TemplateTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "M0 {A1}")]
        public static partial void M0(ILogger logger, int a1);

        [LoggerMessage(1, LogLevel.Error, "M1 {A1} {A1}")]
        public static partial void M1(ILogger logger, int a1);

        [LoggerMessage(2, LogLevel.Error, "M2 {A1} {a2} {A3} {a4} {A5} {a6} {A7}")]
        public static partial void M2(ILogger logger, int a1, int a2, int a3, int a4, int a5, int a6, int a7);

        [LoggerMessage(3, LogLevel.Error, "M3 {a2} {A1}")]
        public static partial void M3(ILogger logger, int a1, int a2);
    }
}
