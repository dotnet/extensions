// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    // test that particular method signature variations are generated correctly
    internal static partial class SignatureTestExtensions
    {
        // extension method
        [LogMethod(eventId: 10, level: LogLevel.Critical, message: "Message11")]
        internal static partial void M11(this ILogger logger);

        // optional parameter
        [LogMethod(1, LogLevel.Debug, "{p1} {p2}")]
        internal static partial void M2(ILogger logger, string p1, string p2 = "World");

        // parameter name potentially conflicting with generated symbol name
        [LogMethod(2, LogLevel.Debug, "{helper}")]
        internal static partial void M3(ILogger logger, string helper);
    }

    // test that particular method signature variations are generated correctly
    internal partial class SignatureTestExtensions<T>
        where T : class
    {
        public static void Combo(ILogger logger, ILogger<int> logger2)
        {
            M1(logger);
            M2(logger);
            M3(logger);
            M4(logger2);
            M5(logger, new[] { "A" });
            M6(logger);
            M8(logger);
            M9(logger);
            M10(logger, null);
            M11(logger, "A", LogLevel.Debug, "B");
        }

        // normal public method
        [LogMethod(0, LogLevel.Critical, "Message1")]
        public static partial void M1(ILogger logger);

        // internal method
        [LogMethod(1, LogLevel.Critical, "Message2")]
        internal static partial void M2(ILogger logger);

        // private method
        [LogMethod(2, LogLevel.Critical, "Message3")]
        private static partial void M3(ILogger logger);

        // generic ILogger
        [LogMethod(3, LogLevel.Critical, "Message4")]
        private static partial void M4(ILogger<int> logger);

        // random type method parameter
        [LogMethod(4, LogLevel.Critical, "Message5 {items}")]
        private static partial void M5(ILogger logger, System.Collections.IEnumerable items);

        // line feeds and quotes in the message string
        [LogMethod(5, LogLevel.Critical, "Message6\n\"\r")]
        private static partial void M6(ILogger logger);

        // generic parameter
        [LogMethod(6, LogLevel.Critical, "Message7 {p1}\n\"\r")]
        private static partial void M7(ILogger logger, T p1);

        // normal public method
        [LogMethod(7, LogLevel.Critical, "Message8")]
        private protected static partial void M8(ILogger logger);

        // internal method
        [LogMethod(8, LogLevel.Critical, "Message9")]
        protected internal static partial void M9(ILogger logger);

        // nullable parameter
        [LogMethod(9, LogLevel.Critical, "Message10 {optional}")]
        internal static partial void M10(ILogger logger, string? optional);

        // dynamic log level
        [LogMethod(10, "Message11 {p1} {p2}")]
        internal static partial void M11(ILogger logger, string p1, LogLevel level, string p2);
    }
}
