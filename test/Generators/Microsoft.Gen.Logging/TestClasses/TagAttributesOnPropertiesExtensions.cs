// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Gen.Logging.Test;

namespace TestClasses
{
#pragma warning disable SA1402 // File may only contain a single type

    internal static partial class TagAttributesOnPropertiesExtensions
    {
        [LoggerMessage(0, LogLevel.Information, "Testing TagName and TagProvider attributes on properties...")]
        internal static partial void LogObjectWithTagAttributes(
            ILogger logger,
            [LogProperties] ClassToLogWithTagAttributes objectToLog);

        [LoggerMessage(1, LogLevel.Information, "Testing TagName and TagProvider attributes on properties of an object from another assembly...")]
        internal static partial void LogObjectWithTagAttributesFromAnotherAssembly(
            ILogger logger,
            [LogProperties] ObjectToLogWithTagAttributes objectToLog);

        [LoggerMessage(2, LogLevel.Information, "Testing TagName and TagProvider attributes on properties when null properties are skipped...")]
        internal static partial void LogObjectWithTagAttributesSkipNulls(
            ILogger logger,
            [LogProperties(SkipNullProperties = true)] ClassToLogWithTagAttributes objectToLog);
    }

    internal sealed class ClassToLogWithTagAttributes
    {
        [TagName("property.to.log")]
        public string? PropertyToLog { get; set; }

        [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideTags))]
        public string StringPropertyToProvide { get; set; } = "default";

        [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideForInt))]
        public int IntPropertyToProvide { get; set; }

        [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideForNullableClass))]
        public PropertyToProvide? PropertyToProvide { get; set; }

        [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideForNullableClass), OmitReferenceName = true)]
        public PropertyToProvide? OmittedPropertyToProvide { get; set; }
    }

    internal sealed class PropertyToProvide
    {
        public string? Value { get; set; }
    }

#pragma warning restore SA1402 // File may only contain a single type
}
