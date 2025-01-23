// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test code")]
    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Test code")]
    [SuppressMessage("Major Code Smell", "S2376:Write-only properties should not be used", Justification = "Test code")]
    internal static partial class LogPropertiesExtensions
    {
        public delegate void TestDelegate();

        internal class MyBaseClass
        {
            public virtual string VirtualPropertyBase => "Hello from MyBaseClass!"; // Not supposed to be logged (overridden in both MyDerivedClass and MyInterimClass)

            public string? NonVirtualPropertyBase { get; set; }

            public string? StringPropertyBase { get; set; }

            protected int ProtectedPopertyBase { get; set; } // Not supposed to be logged (protected)

            private int PrivatePopertyBase { get; set; } // Not supposed to be logged (private)

            public Action? ActionBase { get; set; } // Not supposed to be logged (delegate type)

            private Func<int>? FuncBase { get; set; } // Not supposed to be logged (delegate type)

            private TestDelegate? DelegateBase { get; set; } // Not supposed to be logged (delegate type)
        }

        internal class MyInterimClass : MyBaseClass
        {
            public virtual int VirtualInterimProperty { get; set; } // Not supposed to be logged (overridden in MyDerivedClass)

            public long InterimProperty { get; set; }

            public override string VirtualPropertyBase => "Hello from MyInterimClass!"; // Not supposed to be logged (overridden in MyDerivedClass)
        }

        public class MyTransitiveBaseClass
        {
            public decimal TransitiveNumberProp { get; set; } = decimal.One;

            public string? TransitiveStringProp { get; set; }

            public virtual int TransitiveVirtualProp { get; set; } // Not supposed to be logged (overridden in MyTransitiveDerivedClass)

            [LogProperties]
            public GenericClass<string> TransitiveGenericProp => new()
            {
                GenericProp = "Hello from MyTransitiveBaseClass!"
            };

            internal double TransitiveInternalProp { get; set; } // Not supposed to be logged (internal)

            public double TransitiveField = double.PositiveInfinity; // Not supposed to be logged (field)

            private static decimal PrivateProperty => decimal.MinusOne; // Not supposed to be logged (private & static)

            [LogProperties]
            public LeafTransitiveDerivedClass? InnerTransitiveProperty { get; set; }
        }

        public class MyTransitiveDerivedClass : MyTransitiveBaseClass
        {
            public override int TransitiveVirtualProp { get; set; } // Overrides MyTransitiveBaseClass.TransitiveVirtualProp

            public int TransitiveDerivedProp { get; set; }
        }

        public class LeafTransitiveBaseClass
        {
            public int IntegerProperty { get; set; } = int.MaxValue;
        }

        public class LeafTransitiveDerivedClass : LeafTransitiveBaseClass
        {
            public DateTime DateTimeProperty { get; set; } = DateTime.MaxValue;
        }

        public class GenericClass<T>
        {
            public T? GenericProp { get; set; }
        }

        public struct MyCustomStruct
        {
            public long LongProperty { get; set; } = long.MaxValue;

#pragma warning disable CA1805 // Do not initialize unnecessarily
            [LogProperties]
            public MyTransitiveStruct TransitiveStructProperty { get; set; } = default;

            [LogProperties]
            public MyTransitiveStruct? NullableTransitiveStructProperty { get; set; } = default;
#pragma warning restore CA1805 // Do not initialize unnecessarily

            [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1125:Use shorthand for nullable types", Justification = "Testing Nullable<T>")]
            [LogProperties]
            public Nullable<MyTransitiveStruct> NullableTransitiveStructProperty2 { get; set; } = default;

            public MyCustomStruct(object _)
            {
            }
        }

        public class MyClassWithNullableProperty
        {
            public DateTime? NullableDateTime { get; set; }
            public DateTime NonNullableDateTime { get; set; }
        }

        public struct MyTransitiveStruct
        {
            public DateTimeOffset DateTimeOffsetProperty { get; set; } = DateTimeOffset.UtcNow;

            public MyTransitiveStruct(object _)
            {
            }
        }

        internal class MyDerivedClass : MyInterimClass
        {
            public static int StaticNumberProperty { get; set; } = ushort.MaxValue; // Not supposed to be logged (static)

            public string? StringProperty { get; set; }

            public int? SimplifiedNullableIntProperty { get; set; }

            [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1125:Use shorthand for nullable types", Justification = "Test code")]
            public Nullable<int> ExplicitNullableIntProperty { get; set; }

            // Not supposed to be logged (private getter)
            public string PrivateGetStringProperty { private get; set; } = nameof(PrivateGetStringProperty);

            public DateTime GetOnlyProperty => DateTime.MaxValue;

            public DateTime SetOnlyProperty // Not supposed to be logged (write-only property)
            {
                set => _ = value; // No-op
            }

            private static decimal PrivateProperty => decimal.MinusOne; // Not supposed to be logged (private & static)

            internal string InternalProperty { get; set; } = nameof(InternalProperty); // Not supposed to be logged (internal)

            public string PublicField = nameof(PublicField); // Not supposed to be logged (field)

            private readonly double _privateField; // Not supposed to be logged (private & field)

            public override string VirtualPropertyBase => "Hello from MyDerivedClass!"; // Overrides MyBaseClass.VirtualPropertyBase

            public override int VirtualInterimProperty // Overrides MyInterimClass.VirtualInterimProperty
            {
                get => base.VirtualInterimProperty + 10;
                set => base.VirtualInterimProperty = value - 10;
            }

            public int[] TransitivePropertyArray { get; set; } = Array.Empty<int>();

            [LogProperties]
            public MyTransitiveDerivedClass? TransitiveProperty { get; set; }

            [LogProperties]
            public LeafTransitiveBaseClass? AnotherTransitiveProperty { get; set; }

            [LogProperties]
            public GenericClass<int>? PropertyOfGenerics { get; set; }

            [LogProperties]
            public MyCustomStruct CustomStructProperty { get; set; }

            [LogProperties]
            public MyCustomStruct? CustomStructNullableProperty { get; set; }

            [LogPropertyIgnore]
            public int IgnoredProp { get; set; }

            [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1125:Use shorthand for nullable types", Justification = "Testing Nullable<T>")]
            [LogProperties]
            public Nullable<MyCustomStruct> CustomStructNullableProperty2 { get; set; }

            public MyDerivedClass(double privateFieldValue)
            {
                _privateField = privateFieldValue;
            }
        }

        internal interface IMyInterface
        {
            int IntProperty { get; set; }

            [LogProperties]
            LeafTransitiveBaseClass? TransitiveProp { get; set; }
        }

        internal sealed class MyInterfaceImpl : IMyInterface
        {
            public int IntProperty { get; set; }

            public string? ClassStringProperty { get; set; }

            [LogProperties]
            public LeafTransitiveBaseClass? TransitiveProp { get; set; }
        }

        [LoggerMessage(0, LogLevel.Debug, "Only {classToLog_StringProperty_1} as param")]
        public static partial void LogFunc(ILogger logger, string classToLog_StringProperty_1, [LogProperties] MyDerivedClass classToLog);

        internal class ClassAsParam
        {
            public int MyProperty { get; set; }

            public override string ToString()
                => DateTime
                    .Parse("2021-11-15", CultureInfo.InvariantCulture)
                    .ToString("D", CultureInfo.InvariantCulture);
        }

        [LoggerMessage(1, LogLevel.Information, "Both {StringProperty} and {ComplexParam} as params")]
        public static partial void LogMethodTwoParams(ILogger logger, string StringProperty, [LogProperties] ClassAsParam? complexParam);

        [LoggerMessage(2, LogLevel.Information, "Testing non-nullable struct here...")]
        public static partial void LogMethodStruct(ILogger logger, [LogProperties] MyCustomStruct structParam);

        [LoggerMessage(3, LogLevel.Information, "Testing nullable struct here...")]
        public static partial void LogMethodNullableStruct(ILogger logger, [LogProperties] in MyCustomStruct? structParam);

        [LoggerMessage(4, LogLevel.Information, "Testing explicit nullable struct here...")]
        public static partial void LogMethodExplicitNullableStruct(ILogger logger, [LogProperties] Nullable<MyCustomStruct> structParam);

        [LoggerMessage(5, LogLevel.Information, "Testing nullable property within class here...")]
        public static partial void LogMethodNullablePropertyInClassMatchesNonNullable(ILogger logger, [LogProperties] MyClassWithNullableProperty classWithNullablePropertyParam);

        [LoggerMessage]
        public static partial void LogMethodDefaultAttrCtor(ILogger logger, LogLevel level, [LogProperties] ClassAsParam? complexParam);

        [LoggerMessage(6, LogLevel.Information, "Testing interface-typed argument here...")]
        public static partial void LogMethodInterfaceArg(ILogger logger, [LogProperties] IMyInterface complexParam);
    }
}
