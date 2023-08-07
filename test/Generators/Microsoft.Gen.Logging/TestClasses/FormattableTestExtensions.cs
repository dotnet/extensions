// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class FormattableTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "Method1 {p1}")]
        public static partial void Method1(ILogger logger, Formattable p1);

        [LoggerMessage(1, LogLevel.Error, "Method2")]
        public static partial void Method2(ILogger logger, [LogProperties] ComplexObj p1);

        internal class Formattable : IFormattable
        {
            public string ToString(string? format, IFormatProvider? formatProvider)
            {
                return "Formatted!";
            }
        }

        internal class ComplexObj
        {
            public Formattable P1 { get; } = new Formattable();
        }
    }
}
