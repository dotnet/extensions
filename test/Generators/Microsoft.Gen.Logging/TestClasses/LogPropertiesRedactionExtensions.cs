// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    internal static partial class LogPropertiesRedactionExtensions
    {
        internal class MyBaseClassToRedact
        {
            [PrivateData]
            public string StringPropertyBase { get; set; } = "StringPropertyBase";
        }

        internal class MyInterimClassToRedact : MyBaseClassToRedact
        {
            public string NoRedactionProp { get; set; } = "No redaction";
        }

        internal class ClassToRedact : MyInterimClassToRedact
        {
            [PublicData]
            public string StringProperty { get; set; } = "StringProperty";

            public int SimplifiedNullableIntProperty { get; set; } = int.MinValue;

            [PrivateData]
            public string GetOnlyProperty => "GetOnlyProperty";

            public MyTransitiveClass TransitiveProp { get; set; } = new();
        }

        public class MyTransitiveClass
        {
            public int TransitiveNumberProp { get; set; } = int.MaxValue;

            [PrivateData]
            public string TransitiveStringProp { get; set; } = "TransitiveStringProp";
        }

        [LogMethod(1, LogLevel.Debug, "No template params")]
        public static partial void LogNoParams(ILogger logger, IRedactorProvider redactionProvider, [LogProperties] ClassToRedact classToLog);

        [LogMethod(2, LogLevel.Information, "Only {StringProperty} as param")]
        public static partial void LogTwoParams(
            ILogger logger, IRedactorProvider redactionProvider,
            [PrivateData] string stringProperty, [LogProperties] MyTransitiveClass? complexParam);

        // Default ctors:
        [LogMethod]
        public static partial void LogNoParamsDefaultCtor(ILogger logger, LogLevel level,
            IRedactorProvider redactionProvider, [LogProperties] ClassToRedact classToLog);

        [LogMethod]
        public static partial void LogTwoParamsDefaultCtor(
            ILogger logger, IRedactorProvider redactionProvider, LogLevel level,
            [PrivateData] string stringProperty, [LogProperties] MyTransitiveClass? complexParam);
    }
}
