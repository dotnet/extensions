// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    public partial class NonStaticTestClass
    {
        private readonly ILogger _logger;

        public NonStaticTestClass(ILogger logger)
        {
            _logger = logger;
        }

        [LogMethod(0, LogLevel.Debug, "M0 {p0}")]
        public partial void M0([In] string p0);

        [LogMethod(1, LogLevel.Debug, "M1 {p0}")]
        public partial void M1([PrivateData] string p0);

        [LogMethod(2, LogLevel.Debug, "M2 {p0} {p1} {p2}")]
        public partial void M2([PrivateData] string p0, [PrivateData] string p1, [PrivateData] string p2);

        [LogMethod]
        public partial void M3(LogLevel level, [PrivateData] string p0);

        [LogMethod(4, LogLevel.Information, "LogProperties: {P0}")]
        internal partial void LogProperties(string p0, [LogProperties] ClassToLog p1);

        [LogMethod(5, LogLevel.Information, "LogProperties with provider: {P0}, {P1}")]
        internal partial void LogPropertiesWithProvider(
            string p0,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideProperties))] ClassToLog p1);

        [LogMethod(6, LogLevel.Information, "LogProperties with redaction: {P0}")]
        internal partial void LogPropertiesWithRedaction(
            [PrivateData] string p0,
            [LogProperties] LogPropertiesRedactionExtensions.MyBaseClassToRedact p1);

        [LogMethod]
        internal partial void DefaultAttrCtorLogPropertiesWithProvider(
            LogLevel level,
            string p0,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideProperties))] ClassToLog p1);

        [LogMethod]
        internal partial void DefaultAttrCtorLogPropertiesWithRedaction(
            LogLevel level,
            [PrivateData] string p0,
            [LogProperties] LogPropertiesRedactionExtensions.MyBaseClassToRedact p1);

        [LogMethod(7, LogLevel.Warning, "No params here...")]
        public partial void NoParams();

        [LogMethod(8, "No params here as well...")]
        public partial void NoParamsWithLevel(LogLevel level);
    }
}
