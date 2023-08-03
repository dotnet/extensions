// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class LogPropertiesOmitParameterNameExtensions
    {
        internal class MyProps
        {
            public int P0 { get; set; }

            public string? P1 { get; set; }
        }

        [LogMethod(0, LogLevel.Debug)]
        public static partial void M0(ILogger logger, [LogProperties(OmitParameterName = true)] MyProps p);

        [LogMethod(1, LogLevel.Warning)]
        public static partial void M1(
            ILogger logger,
            [LogProperties(typeof(MyPropsProvider), nameof(MyPropsProvider.ProvideProperties), OmitParameterName = true)] MyProps p);

        [LogMethod]
        internal static partial void M2(
            ILogger logger,
            LogLevel level,
            [LogProperties(OmitParameterName = true)] MyProps param);

        [LogMethod]
        internal static partial void M3(
            ILogger logger,
            LogLevel level,
            [LogProperties(typeof(MyPropsProvider), nameof(MyPropsProvider.ProvideProperties), OmitParameterName = true)] MyProps p);

        internal static class MyPropsProvider
        {
            public static void ProvideProperties(ITagCollector list, MyProps? param)
            {
                list.Add(nameof(MyProps.P0), param?.P0);
                list.Add("Custom_property_name", param?.P1);
            }
        }
    }
}
