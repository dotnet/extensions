// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Gen.Logging.Parsing;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public partial class ParserTests
{
    [Fact]
    public async Task InvalidTagProviderUsage()
    {
        await RunGenerator(@"
            class MyClass
            {
                [/*0+*/TagProvider(typeof(Provider), ""Provide"")/*-0*/]
                internal string P0 { get; set; }

                [/*1+*/TagProvider(typeof(Provider), ""Provide"")/*-1*/]
                internal static string P1 { get; set; }

                [/*2+*/TagProvider(typeof(Provider), ""Provide"")/*-2*/]
                internal string P2 { set; }

                [/*3+*/TagProvider(typeof(Provider), ""Provide"")/*-3*/]
                public string P3 { internal get; set; }
            }

            static class Provider
            {
                public static void Provide(ITagCollector collector, string p1) { }
            }

            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M0(ILogger logger, [LogProperties] MyClass p1);
            }", DiagDescriptors.InvalidAttributeUsage);
    }

    [Fact]
    public async Task TooManyAttributes()
    {
        await RunGenerator(@"
            class MyClass
            {
                public string Property { get; set; }
            }

            class MyClass2
            {
                [LogProperties, TagProvider(typeof(Provider), ""Provide"")]
                public MyClass /*0+*/Property1/*-0*/ { get; set; } = new();

                [LogPropertyIgnore, TagProvider(typeof(Provider), ""Provide"")]
                public MyClass /*1+*/Property2/*-1*/ { get; set; } = new();

                public int Property3 { get; set; }
            }

            static class Provider
            {
                public static void Provide(ITagCollector collector, MyClass p1) { }
            }

            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M0(ILogger logger, [LogProperties, TagProvider(typeof(Provider), nameof(Provider.Provide))] MyClass /*2+*/p1/*-2*/);

                [LoggerMessage(1, LogLevel.Debug, ""Parameter"")]
                static partial void M1(ILogger logger, [LogProperties] MyClass2 p1);
            }", DiagDescriptors.CantMixAttributes);
    }

    [Fact]
    public async Task TagProviderTypeNotFound()
    {
        await RunGenerator(@"
            class MyClass
            {
                public string Property { get; set; }
            }

            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [TagProvider(typeof(XXX), """")] MyClass p1);
            }");
    }

    [Fact]
    public async Task TagProviderOnUnsupportedParameters()
    {
        string source = @"
            class MyClass
            {
                public string Property { get; set; }
            }

            static class Provider
            {
                public static void Provide(ITagCollector props, MyClass? value)
                {
                }
            }

            partial class C
            {
                [LoggerMessage(""Hello"")]
                static partial void M0(ILogger logger, [TagProvider(typeof(Provider), ""Provide"")] LogLevel /*0+*/l1/*-0*/);

                [LoggerMessage(LogLevel.Debug)]
                static partial void M1(ILogger logger, [TagProvider(typeof(Provider), ""Provide"")] global::System.Exception /*1+*/ex/*-1*/);

                [LoggerMessage(LogLevel.Debug)]
                static partial void M2([TagProvider(typeof(Provider), ""Provide"")] ILogger /*2+*/logger/*-2*/, string p0);
            }";

        await RunGenerator(source, DiagDescriptors.TagProviderInvalidUsage);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"Error\"")]
    [InlineData("\"Prop\"")]
    [InlineData("\"Field\"")]
    [InlineData("\"Const\"")]
    public async Task TagProviderMethodNotFound(string methodName)
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
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/TagProvider(typeof(Provider), {methodName})/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.TagProviderMethodNotFound);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"Error\"")]
    [InlineData("\"Prop\"")]
    [InlineData("\"Field\"")]
    [InlineData("\"Const\"")]
    public async Task TagProviderMethodNotFoundNested(string methodName)
    {
        string source = @$"
            class MyClass
            {{
                public string Property {{ get; set; }}

                [/*0+*/TagProvider(typeof(Provider), {methodName})/*-0*/]
                public string AnotherProperty {{ get; set; }}
            }}

            static class Provider
            {{
                public static string Prop {{ get; set; }}
                public static string Field;
                public static const string Const = ""test"";
            }}

            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [LogProperties] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.TagProviderMethodNotFound);
    }

    [Fact]
    public async Task TagProviderMethodNotFound2()
    {
        const string Source = @"
            class MyClass
            {
                public string Property { get; set; }
            }

            static class Provider
            {
                public static void Provide1(ITagCollector props, MyClass? value)
                {
                }

                public static void Provide2(ITagCollector props, MyClass? value, int a)
                {
                }
            }

            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/TagProvider(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }";

        await RunGenerator(Source, DiagDescriptors.TagProviderMethodNotFound);
    }

    [Fact]
    public async Task TagProviderMethodIsGeneric()
    {
        const string Source = @"
            class MyClass
            {
                public string Property { get; set; }
            }

            static class Provider
            {
                public static void Provide<T>(ITagCollector props, MyClass? value)
                {
                }
            }

            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/TagProvider(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }";

        await RunGenerator(Source, DiagDescriptors.TagProviderMethodInvalidSignature);
    }

    [Fact]
    public async Task TagProvider_UsingInterfacesAndBaseClassAndNullableAndOptional()
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
                public static void Provide1(ITagCollector props, MyClass? value) {}
                public static void Provide2(ITagCollector props, BaseClass value) {}
                public static void Provide3(ITagCollector props, IFoo value) {}
                public static void Provide4(ITagCollector props, MyClass value, object o = null) {}
                public static void Provide5(ITagCollector props, MyClass value) {}
            }

            partial class C
            {
                [LoggerMessage(LogLevel.Debug)]
                static partial void M1(ILogger logger, [TagProvider(typeof(Provider), nameof(Provider.Provide1))] MyClass p1);

                [LoggerMessage(LogLevel.Debug)]
                static partial void M2(ILogger logger, [TagProvider(typeof(Provider), nameof(Provider.Provide2))] MyClass p1);

                [LoggerMessage(LogLevel.Debug)]
                static partial void M3(ILogger logger, [TagProvider(typeof(Provider), nameof(Provider.Provide3))] MyClass p1);

                [LoggerMessage(LogLevel.Debug)]
                static partial void M4(ILogger logger, [TagProvider(typeof(Provider), nameof(Provider.Provide4))] MyClass p1);

                [LoggerMessage(LogLevel.Debug)]
                static partial void M5(ILogger logger, [/*0+*/TagProvider(typeof(Provider), nameof(Provider.Provide5))/*-0*/] MyClass? p1);
            }";

        await RunGenerator(Source, DiagDescriptors.TagProviderMethodInvalidSignature);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ITagCollector props")]
    [InlineData("ITagCollector props, MyClass? value, int a")]
    public async Task TagProviderMethodParamsCount(string paramsList)
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
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/TagProvider(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.TagProviderMethodInvalidSignature);
    }

    [Theory]
    [CombinatorialData]
    public async Task TagProviderMethodParamsRefKind(
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
                public static void Provide({listModifier} ITagCollector props, {valueModifier} MyClass? value)
                {{
                }}
            }}

            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/TagProvider(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.TagProviderMethodInvalidSignature);
    }

    [Theory]
    [CombinatorialData]
    public async Task TagProviderMethodParamsInvalidType(
        [CombinatorialValues("ITagCollector", "MyClass?", "int", "object", "string", "DateTime")] string listType,
        [CombinatorialValues("ITagCollector", "int", "string", "DateTime")] string valueType)
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
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/TagProvider(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.TagProviderMethodInvalidSignature);
    }

    [Theory]
    [InlineData("private")]
    [InlineData("")]
    public async Task TagProviderMethodIsInaccessible(string methodModifier)
    {
        string source = @$"
            class MyClass
            {{
                public string Property {{ get; set; }}
            }}

            static class Provider
            {{
                {methodModifier} static void Provide(ITagCollector props, MyClass? value)
                {{
                    return 0;
                }}
            }}

            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M(ILogger logger, [/*0+*/TagProvider(typeof(Provider), nameof(Provider.Provide))/*-0*/] MyClass p1);
            }}";

        await RunGenerator(source, DiagDescriptors.TagProviderMethodInaccessible);
    }
}
