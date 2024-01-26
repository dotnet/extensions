// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Text;

namespace TestClasses
{
#pragma warning disable SA1402 // File may only contain a single type

    internal static partial class TagProviderExtensions
    {
        [LoggerMessage(int.MaxValue, LogLevel.Warning, "Custom provided properties for {Param}.")]
        internal static partial void LogMethodCustomPropsProvider(
            ILogger logger,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideTags))] ClassToLog param);

        [LoggerMessage(LogLevel.Debug, "Custom provided properties for struct.")]
        internal static partial void LogMethodCustomPropsProviderStruct(
            ILogger logger,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideForStruct))] StructToLog param);

        [LoggerMessage(LogLevel.Information, "Custom provided properties for interface.")]
        internal static partial void LogMethodCustomPropsProviderInterface(
            ILogger logger,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideForInterface))] IInterfaceToLog param);

        [LoggerMessage(int.MinValue, LogLevel.Warning, "Custom provided properties for both complex params and {StringParam}.")]
        internal static partial void LogMethodCustomPropsProviderTwoParams(
            ILogger logger,
            string stringParam,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideTags))] ClassToLog param,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideOtherTags))] ClassToLog param2);

        [LoggerMessage(1, LogLevel.Warning, "No params.")]
        internal static partial void LogMethodCombinePropsProvider(
            ILogger logger,
            [LogProperties] ClassToLog param1,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideTags))] ClassToLog param2);

        [LoggerMessage]
        internal static partial void DefaultAttributeCtor(
            ILogger logger,
            LogLevel level,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideTags))] ClassToLog param);

        [LoggerMessage]
        internal static partial void Nullable(
            ILogger logger,
            LogLevel level,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideTags))] int? param);

        [LoggerMessage]
        internal static partial void Enumerable(
            ILogger logger,
            LogLevel level,
            [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideForEnumerable))] IEnumerable<string> things);
    }

    internal static class CustomProvider
    {
        public static void ProvideTags(ITagCollector list, ClassToLog? param)
        {
            // This condition is here only for testing purposes:
            if (param is null)
            {
                return;
            }

            list.Add(nameof(ClassToLog.MyIntProperty), param.MyIntProperty);
            list.Add("Custom_property_name", param.MyStringProperty);
        }

        public static void ProvideTags(ITagCollector list, int? p)
        {
            if (p != null)
            {
                list.Add("P1", p.Value.ToInvariantString());
            }
        }

        public static void ProvideTags(ITagCollector list, string v)
        {
            list.Add("NestedTagProvider", v);
        }

        public static void ProvideOtherTags(ITagCollector list, ClassToLog? param)
        {
            list.Add("Another_property_name", param?.MyStringProperty.ToUpperInvariant());
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

        public static void ProvideForEnumerable(ITagCollector list, IEnumerable<string> e)
        {
            int i = 0;
            foreach (var s in e)
            {
                list.Add($"Foo{i}", s);
                i++;
            }
        }
    }

    internal sealed class ClassToLog
    {
        public int MyIntProperty { get; set; }

        public string MyStringProperty { get; set; } = "Test string";

        [TagProvider(typeof(CustomProvider), nameof(CustomProvider.ProvideTags))]
        public string AnotherStringProperty { get; set; } = "Another test string";

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

        public override readonly string ToString() => "Custom struct string representation";
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
