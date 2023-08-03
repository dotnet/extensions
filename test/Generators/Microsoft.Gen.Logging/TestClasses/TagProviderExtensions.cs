// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
#pragma warning disable SA1402 // File may only contain a single type

    internal static partial class TagProviderExtensions
    {
        [LogMethod(int.MaxValue, LogLevel.Warning, "Custom provided properties for {Param}.")]
        internal static partial void LogMethodCustomPropsProvider(
            ILogger logger,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideProperties))] ClassToLog param);

        [LogMethod(LogLevel.Debug, "Custom provided properties for struct.")]
        internal static partial void LogMethodCustomPropsProviderStruct(
            ILogger logger,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideForStruct))] StructToLog param);

        [LogMethod(LogLevel.Information, "Custom provided properties for interface.")]
        internal static partial void LogMethodCustomPropsProviderInterface(
            ILogger logger,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideForInterface))] IInterfaceToLog param);

        [LogMethod(int.MinValue, LogLevel.Warning, "Custom provided properties for both complex params and {StringParam}.")]
        internal static partial void LogMethodCustomPropsProviderTwoParams(
            ILogger logger,
            string stringParam,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideProperties))] ClassToLog param,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideOtherProperties))] ClassToLog param2);

        [LogMethod(1, LogLevel.Warning, "No params.")]
        internal static partial void LogMethodCombinePropsProvider(
            ILogger logger,
            [LogProperties] ClassToLog param1,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideProperties))] ClassToLog param2);

        [LogMethod]
        internal static partial void DefaultAttributeCtor(
            ILogger logger,
            LogLevel level,
            [LogProperties(typeof(CustomProvider), nameof(CustomProvider.ProvideProperties))] ClassToLog param);
    }

    internal static class CustomProvider
    {
        public static void ProvideProperties(ITagCollector list, ClassToLog? param)
        {
            // This condition is here only for testing purposes:
            if (param is null)
            {
                return;
            }

            list.Add(nameof(ClassToLog.MyIntProperty), param.MyIntProperty);
            list.Add("Custom_property_name", param.MyStringProperty);
        }

        public static void ProvideOtherProperties(ITagCollector list, ClassToLog? param)
        {
            list.Add("Another_property_name", param?.MyStringProperty?.ToUpperInvariant());
            list.Add(nameof(ClassToLog.MyIntProperty) + "_test", param?.MyIntProperty);
        }

        public static void ProvideForStruct(ITagCollector list, StructToLog param)
        {
            list.Add(nameof(ClassToLog.MyIntProperty), param.MyIntProperty);
            list.Add("Custom_property_name", param.MyStringProperty);
        }

        public static void ProvideForInterface(ITagCollector list, IInterfaceToLog param)
        {
            list.Add(nameof(ClassToLog.MyIntProperty), param.MyIntProperty);
            list.Add("Custom_property_name", param.MyStringProperty);
        }
    }

    internal sealed class ClassToLog
    {
        public int MyIntProperty { get; set; }

        public string MyStringProperty { get; set; } = "Test string";

        public override string ToString() => "Custom string representation";
    }

    internal struct StructToLog
    {
        public StructToLog()
        {
            MyStringProperty = "Test string from struct";
        }

        public int MyIntProperty { get; set; }

        public string MyStringProperty { get; set; }

        public override string ToString() => "Custom struct string representation";
    }

    internal interface IInterfaceToLog
    {
        int MyIntProperty { get; set; }

        string MyStringProperty { get; set; }
    }

    internal sealed class InterfaceImpl : IInterfaceToLog
    {
        public int MyIntProperty { get; set; }

        string IInterfaceToLog.MyStringProperty { get; set; } = "Test string from interface implementation";
    }
}
