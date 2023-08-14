// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
#pragma warning disable SA1402 // File may only contain a single type

    internal static partial class TagProviderWithObjectExtensions
    {
        [LoggerMessage(int.MaxValue, LogLevel.Warning, "Custom provided properties for {Param}.")]
        internal static partial void OneParam(
            ILogger logger,
            [LogProperties(typeof(CustomProviderWithObject), nameof(CustomProviderWithObject.ProvideProperties))] object param);

        [LoggerMessage(int.MinValue, LogLevel.Warning, "Custom provided properties for both complex params and {StringParam}.")]
        internal static partial void TwoParams(
            ILogger logger,
            string stringParam,
            [LogProperties(typeof(CustomProviderWithObject), nameof(CustomProviderWithObject.ProvideProperties))] object param,
            [LogProperties(typeof(CustomProviderWithObject), nameof(CustomProviderWithObject.ProvideOtherProperties))] object param2);
    }

    internal static class CustomProviderWithObject
    {
        public static void ProvideProperties(ITagCollector list, object? param)
        {
            // This condition is here only for testing purposes:
            if (param is null)
            {
                return;
            }

            list.Add(nameof(object.ToString), param + " ProvidePropertiesCall");
        }

        public static void ProvideOtherProperties(ITagCollector list, object? param)
        {
            list.Add(nameof(object.ToString), param + " ProvideOtherPropertiesCall");
            list.Add("Type", param?.GetType());
        }
    }
}
