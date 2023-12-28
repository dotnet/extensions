// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class TransitiveTestExtensions
    {
        public class C0
        {
            public C1 P0 { get; set; } = new C1();
            public string P1 { get; set; } = "V1";
        }

        public class C1
        {
            public string P2 { get; set; } = "V2";
            public override string ToString() => "TS1";
        }

        [LoggerMessage(LogLevel.Debug)]
        public static partial void M0(ILogger logger, [LogProperties(Transitive = true)] C0 p0);

        [LoggerMessage(LogLevel.Debug)]
        public static partial void M1(ILogger logger, [LogProperties(Transitive = false)] C0 p0);
    }
}
