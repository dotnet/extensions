// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Gen.Logging.Parsing;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public partial class ParserTests
{
    [Fact]
    public static async Task LogPropertiesOmitParamName_DetectsNameCollision()
    {
        const string Source = @"
            class MyType
            {
                public int p0 { get; }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""Parameterless..."")]
                static partial void M0(ILogger logger, [LogProperties(OmitParameterName = true)] MyType /*0+*/p0/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesNameCollision);
    }

    [Fact]
    public static async Task LogProperties_AllowsDefaultLogMethodCtor()
    {
        await RunGenerator(@"
            class MyType
            {
                public int p0 { get; }
            }

            partial class C
            {
                [LogMethod]
                static partial void M0(ILogger logger, LogLevel level, [LogProperties] MyType p0);
            }");
    }

    [Fact]
    public static async Task LogProperties_AllowsRecordTypes()
    {
        await RunGenerator(@"
            internal record class MyRecord(int Value)
            {
                public int GetOnlyValue => Value + 1;
            }

            partial class C
            {
                [LogMethod(LogLevel.Debug)]
                public static partial void LogFunc(ILogger logger, [LogProperties] MyRecord p0);
            }");
    }

    [Theory]
    [InlineData("LogLevel")]
    [InlineData("System.Exception")]
    public async Task LogPropertiesInvalidUsage(string annotation)
    {
        // We don't check [LogProperties] on ILogger here since it produces a lot of errors apart from R9G027
        string source = @$"
            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Parameterless..."")]
                static partial void M(ILogger logger, [LogProperties] {annotation} /*0+*/param/*-0*/);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesInvalidUsage);
    }

    [Theory]
    [InlineData("MyClass")]
    [InlineData("MyStruct")]
    [InlineData("MyInterface")]
    public async Task LogPropertiesValidUsage(string parameterType)
    {
        await RunGenerator(@$"
            class MyClass
            {{
                public string Property {{ get; set; }}
            }}

            struct MyStruct
            {{
                public string Property {{ get; set; }}
            }}

            struct MyInterface
            {{
                public string Property {{ get; set; }}
            }}

            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Parameter {{P1}}"")]
                static partial void M(ILogger logger, [LogProperties] {parameterType} p1);
            }}");
    }

    [Theory]
    [InlineData("MyClass")]
    [InlineData("MyStruct")]
    [InlineData("MyInterface")]
    public async Task LogPropertiesParameterSkipped(string parameterType)
    {
        string source = @$"
            class MyClass {{ }}

            struct MyStruct {{ }}

            struct MyInterface {{ }}

            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Empty template"")]
                static partial void M(ILogger logger, [LogProperties] {parameterType} /*0+*/param/*-0*/);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesParameterSkipped);
    }

    [Fact]
    public async Task LogPropertiesParameterNotSkipped()
    {
        await RunGenerator(@"
            class MyClass
            {
                public int A { get; set; }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""{Param} is here"")]
                static partial void M(ILogger logger, [LogProperties] MyClass param);
            }");
    }

    [Fact]
    public async Task LogPropertiesPointlessUsage()
    {
        const string Source = @"
            class MyClass
            {
                private int A { get; set; }
                protected double B { get; set; }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""{param}"")]
                static partial void M(ILogger logger, [LogProperties] MyClass /*0+*/param/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesParameterSkipped);
    }

    [Fact]
    public async Task SimpleNameCollision()
    {
        const string Source = @"
            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""{Param} {Param}"")]
                static partial void M(ILogger logger, string param, string /*0+*/Param/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesNameCollision);
    }

    [Fact]
    public async Task LogPropertiesNameCollision()
    {
        const string Source = @"
            class MyClass
            {
                public int A { get; set; }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""{param_A}"")]
                static partial void M(ILogger logger, string param_A, [LogProperties] MyClass /*0+*/param/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesNameCollision);
    }

    [Fact]
    public async Task LogPropertiesTransitiveNameCollision()
    {
        const string Source = @"
            public class MyClass
            {
                public int Transitive_Prop { get; set; }

                public MyTransitiveClass Transitive { get; set; }
            }

            public class MyTransitiveClass
            {
                public int Prop { get; set; }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties] MyClass /*0+*/param/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesNameCollision);
    }

    [Fact]
    public async Task LogPropertiesProviderTypeNotFound()
    {
        await RunGenerator(@"
            class MyClass
            {
                public string Property { get; set; }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [LogProperties(typeof(XXX), """")] MyClass p1);
            }");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"Error\"")]
    [InlineData("\"Prop\"")]
    [InlineData("\"Field\"")]
    [InlineData("\"Const\"")]
    public async Task LogPropertiesProviderMethodNotFound(string methodName)
    {
        string source = @$"
            class MyClass
            {{
                public string Property {{ get; set; }}
            }}

            static class Provider
            {{
                public static string Prop {{ get; set; }}
                public static string Field;
                public static const string Const = ""test"";
            }}

            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/LogProperties(typeof(Provider), {methodName})/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesProviderMethodNotFound);
    }

    [Fact]
    public async Task LogPropertiesProviderMethodNotFound2()
    {
        const string Source = @"
            class MyClass
            {
                public string Property { get; set; }
            }

            static class Provider
            {
                public static void Provide1(ILogPropertyCollector props, MyClass? value)
                {
                }

                public static void Provide2(ILogPropertyCollector props, MyClass? value, int a)
                {
                }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/LogProperties(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesProviderMethodNotFound);
    }

    [Fact]
    public async Task LogPropertiesProviderMethodIsGeneric()
    {
        const string Source = @"
            class MyClass
            {
                public string Property { get; set; }
            }

            static class Provider
            {
                public static void Provide<T>(ILogPropertyCollector props, MyClass? value)
                {
                }
            }

            partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/LogProperties(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesProviderMethodInvalidSignature);
    }

    [Fact]
    public async Task LogPropertiesProvider_UsingInterfacesAndBaseClassAndNullableAndOptional()
    {
        const string Source = @"
            interface IFoo
            {
            }

            class BaseClass
            {
            }

            class MyClass : BaseClass, IFoo
            {
            }

            static class Provider
            {
                public static void Provide1(ILogPropertyCollector props, MyClass? value) {}
                public static void Provide2(ILogPropertyCollector props, BaseClass value) {}
                public static void Provide3(ILogPropertyCollector props, IFoo value) {}
                public static void Provide4(ILogPropertyCollector props, MyClass value, object o = null) {}
                public static void Provide5(ILogPropertyCollector props, MyClass value) {}
            }

            partial class C
            {
                [LogMethod(LogLevel.Debug)]
                static partial void M1(ILogger logger, [LogProperties(typeof(Provider), nameof(Provider.Provide1))] MyClass p1);

                [LogMethod(LogLevel.Debug)]
                static partial void M2(ILogger logger, [LogProperties(typeof(Provider), nameof(Provider.Provide2))] MyClass p1);

                [LogMethod(LogLevel.Debug)]
                static partial void M3(ILogger logger, [LogProperties(typeof(Provider), nameof(Provider.Provide3))] MyClass p1);

                [LogMethod(LogLevel.Debug)]
                static partial void M4(ILogger logger, [LogProperties(typeof(Provider), nameof(Provider.Provide4))] MyClass p1);

                [LogMethod(LogLevel.Debug)]
                static partial void M5(ILogger logger, [/*0+*/LogProperties(typeof(Provider), nameof(Provider.Provide5))/*-0*/] MyClass? p1);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesProviderMethodInvalidSignature);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ILogPropertyCollector props")]
    [InlineData("ILogPropertyCollector props, MyClass? value, int a")]
    public async Task LogPropertiesProviderMethodParamsCount(string paramsList)
    {
        string source = @$"
            class MyClass
            {{
                public string Property {{ get; set; }}
            }}

            static class Provider
            {{
                public static void Provide({paramsList})
                {{
                }}
            }}

            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/LogProperties(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesProviderMethodInvalidSignature);
    }

    [Theory]
    [CombinatorialData]
    public async Task LogPropertiesProviderMethodParamsRefKind(
        [CombinatorialValues("ref", "out", "in", "")] string listModifier,
        [CombinatorialValues("ref", "out", "in", "")] string valueModifier)
    {
        if (listModifier == string.Empty && valueModifier == string.Empty)
        {
            return;
        }

        string source = @$"
            class MyClass
            {{
                public string Property {{ get; set; }}
            }}

            static class Provider
            {{
                public static void Provide({listModifier} ILogPropertyCollector props, {valueModifier} MyClass? value)
                {{
                }}
            }}

            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/LogProperties(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesProviderMethodInvalidSignature);
    }

    [Theory]
    [CombinatorialData]
    public async Task LogPropertiesProviderMethodParamsInvalidType(
        [CombinatorialValues("ILogPropertyCollector", "MyClass?", "int", "object", "string", "DateTime")] string listType,
        [CombinatorialValues("ILogPropertyCollector", "int", "string", "DateTime")] string valueType)
    {
        string source = @$"
            class MyClass
            {{
                public string Property {{ get; set; }}
            }}

            static class Provider
            {{
                public static void Provide({listType} props, {valueType} value)
                {{
                }}
            }}

            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/LogProperties(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesProviderMethodInvalidSignature);
    }

    [Theory]
    [InlineData("private")]
    [InlineData("")]
    public async Task LogPropertiesProviderMethodIsInaccessible(string methodModifier)
    {
        string source = @$"
            class MyClass
            {{
                public string Property {{ get; set; }}
            }}

            static class Provider
            {{
                {methodModifier} static void Provide(ILogPropertyCollector props, MyClass? value)
                {{
                    return 0;
                }}
            }}

            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/LogProperties(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesProviderMethodInaccessible);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("int?")]
    [InlineData("System.Int32")]
    [InlineData("System.Int32?")]
    [InlineData("bool")]
    [InlineData("bool?")]
    [InlineData("System.Boolean")]
    [InlineData("System.Boolean?")]
    [InlineData("byte")]
    [InlineData("byte?")]
    [InlineData("char?")]
    [InlineData("string")]
    [InlineData("string?")]
    [InlineData("double?")]
    [InlineData("decimal?")]
    [InlineData("object")]
    [InlineData("object?")]
    [InlineData("System.Object")]
    [InlineData("System.Object?")]
    [InlineData("int[]")]
    [InlineData("int?[]")]
    [InlineData("int[]?")]
    [InlineData("int?[]?")]
    [InlineData("object[]")]
    [InlineData("object[]?")]
    [InlineData("System.Array")]
    [InlineData("System.DateTime")]
    [InlineData("System.DateTimeOffset")]
    [InlineData("System.TimeSpan")]
    [InlineData("System.Guid")]
    [InlineData("System.DateTime?")]
    [InlineData("System.DateTimeOffset?")]
    [InlineData("System.TimeSpan?")]
    [InlineData("System.Guid?")]
    [InlineData("System.IDisposable")]
    [InlineData("System.Action")]
    [InlineData("System.Action<int>")]
    [InlineData("System.Func<double>")]
    [InlineData("System.Nullable<int>")]
    [InlineData("System.Nullable<char>")]
    [InlineData("System.Nullable<System.Int32>")]
    [InlineData("System.Nullable<System.Decimal>")]
    [InlineData("System.Nullable<System.DateTime>")]
    [InlineData("System.Nullable<System.DateTimeOffset>")]
    [InlineData("System.Nullable<System.TimeSpan>")]
    [InlineData("System.Nullable<System.Guid>")]
    public async Task IneligibleTypeForPropertiesLogging(string type)
    {
        string source = @$"
            partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties] {type} /*0+*/test/*-0*/);
            }}";

        await RunGenerator(source, DiagDescriptors.InvalidTypeToLogProperties);
    }

    [Theory]
    [InlineData("ClassA")] // Type self-reference
    [InlineData("ClassC")] // One-level cycle
    [InlineData("ClassB")] // Two-level cycle
    [InlineData("StructA")] // Custom struct
    [InlineData("StructA?")] // Nullable struct
    [InlineData("System.Nullable<StructA>")] // Explicit nullable struct
    public async Task PropertyToLogWithTypeCycle(string propertyType)
    {
        string source = @$"
            public class ClassA
            {{
                public {propertyType} Prop {{ get; set; }}
            }}

            public class ClassB
            {{
                public ClassC Prop {{ get; set; }}
            }}

            public class ClassC
            {{
                public ClassA Prop {{ get; set; }}
            }}

            public struct StructA
            {{
                public ClassA Prop {{ get; set; }}
            }}

            partial class LoggerClass
            {{
                [LogMethod(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties] ClassA /*0+*/test/*-0*/);
            }}";

        await RunGenerator(source, DiagDescriptors.LogPropertiesCycleDetected);
    }

    [Fact]
    public async Task PropertyHiddenInDerivedClass()
    {
        const string Source = @"
            public class BaseType
            {
                public int Prop { get; set; }
            }

            public class DerivedType : BaseType
            {
                public new string Prop { get; set; }
            }

            partial class LoggerClass
            {
                [LogMethod(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties] DerivedType /*0+*/test/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesHiddenPropertyDetected);
    }

    [Fact]
    public async Task MultipleDataClassificationAttributes()
    {
        const string Source = @"
            using Microsoft.Extensions.Compliance.Redaction;
            using Microsoft.Extensions.Compliance.Testing;

            class MyClass
            {
                [PrivateData]
                [PrivateData]
                public string? /*0+*/A/*-0*/ { get; set; }
            }

            internal static partial class C
            {
                [LogMethod(0, LogLevel.Debug, ""Only {A}"")]
                static partial void M(ILogger logger, IRedactorProvider provider, [FeedbackData] string a, [LogProperties] MyClass param);
            }";

        await RunGenerator(Source, DiagDescriptors.MultipleDataClassificationAttributes);
    }

    [Theory]
    [InlineData("[PrivateData]", "string")]
    [InlineData("[PrivateData]", "int")]
    public async Task MissingRedactorProvider(string attribute, string type)
    {
        string source = @$"
            using Microsoft.Extensions.Compliance.Redaction;
            using Microsoft.Extensions.Compliance.Testing;

            class MyClass
            {{
                {attribute}
                public {type} A {{ get; set; }}
            }}

            internal static partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""Template..."")]
                static partial void M/*0+*/(ILogger logger, [LogProperties] MyClass param)/*-0*/;
            }}";

        await RunGenerator(source, DiagDescriptors.MissingRedactorProviderArgument);
    }

    [Theory]
    [InlineData("object")]
    [InlineData("object?")]
    [InlineData("System.Object")]
    [InlineData("System.Object?")]
    [InlineData("MyClass")]
    [InlineData("MyStruct")]
    [InlineData("MyInterface")]
    public async Task TypeIsEligibleForLogPropertiesProvider(string type)
    {
        string source = @$"
            internal static partial class C
            {{
                class MyClass {{ }}

                struct MyStruct {{ }}

                struct MyInterface {{ }}

                public static void Provide(ILogPropertyCollector props, {type} value)
                {{
                }}

                [LogMethod(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties(typeof(C), nameof(C.Provide))] {type} test);
            }}";

        await RunGenerator(source);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("int?")]
    [InlineData("System.Int32")]
    [InlineData("System.Int32?")]
    [InlineData("bool")]
    [InlineData("bool?")]
    [InlineData("System.Boolean")]
    [InlineData("System.Boolean?")]
    [InlineData("byte")]
    [InlineData("byte?")]
    [InlineData("char?")]
    [InlineData("string")]
    [InlineData("string?")]
    [InlineData("double?")]
    [InlineData("decimal?")]
    [InlineData("int[]")]
    [InlineData("int?[]")]
    [InlineData("int[]?")]
    [InlineData("int?[]?")]
    [InlineData("object[]")]
    [InlineData("object[]?")]
    [InlineData("System.Array")]
    [InlineData("System.DateTime")]
    [InlineData("System.DateTimeOffset")]
    [InlineData("System.DateTime?")]
    [InlineData("System.DateTimeOffset?")]
    [InlineData("System.IDisposable")]
    [InlineData("System.Action")]
    [InlineData("System.Action<int>")]
    [InlineData("System.Func<double>")]
    [InlineData("System.Nullable<int>")]
    [InlineData("System.Nullable<char>")]
    [InlineData("System.Nullable<System.Int32>")]
    [InlineData("System.Nullable<System.Decimal>")]
    [InlineData("System.Nullable<System.DateTime>")]
    [InlineData("System.Nullable<System.DateTimeOffset>")]
    public async Task IneligibleTypeForLogPropertiesProvider(string type)
    {
        string source = @$"
            internal static partial class C
            {{
                public static void Provide(ILogPropertyCollector props, {type} value)
                {{
                }}

                [LogMethod(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties(typeof(C), nameof(C.Provide))] {type} /*0+*/test/*-0*/);
            }}";

        await RunGenerator(source, DiagDescriptors.InvalidTypeToLogProperties);
    }
}
