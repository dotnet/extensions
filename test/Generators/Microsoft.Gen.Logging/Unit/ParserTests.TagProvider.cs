// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Gen.Logging.Parsing;
using Microsoft.Gen.Shared;
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

    [Fact]
    public async Task TagNameAndTagProviderOnProperties()
    {
        const string Source = @"
            namespace Test
            {
                using Microsoft.Extensions.Logging;

                class MyClass
                {
                    [TagName(""custom.name"")]
                    public string? Property { get; set; }

                    [TagProvider(typeof(Provider), nameof(Provider.Provide))]
                    public PropertyToProvide? PropertyToProvide { get; set; }
                }

                class PropertyToProvide
                {
                    public string? Value { get; set; }
                }

                static class Provider
                {
                    public static void Provide(ITagCollector collector, PropertyToProvide? p)
                    {
                    }
                }

                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                    static partial void M(ILogger logger, [LogProperties] MyClass p1);
                }
            }";

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new LoggingGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(ILogger))!,
                Assembly.GetAssembly(typeof(LoggerMessageAttribute))!,
                Assembly.GetAssembly(typeof(ITagCollector))!,
            },
            [Source],
            []);

        Assert.Empty(d);

        var generatedSource = Assert.Single(r).SourceText.ToString();

        // the [TagName] attribute applied on a property is used as the tag name
        Assert.Contains("\"p1.custom.name\"", generatedSource);

        // the [TagProvider] attribute applied on a property invokes the provider method
        Assert.Contains("state.TagNamePrefix = \"p1.PropertyToProvide\";", generatedSource);
        Assert.Contains("global::Test.Provider.Provide(state, p1?.PropertyToProvide);", generatedSource);
    }

    [Fact]
    public void TagNameAndTagProviderOnPropertiesAreEscapedInGeneratedSource()
    {
        const string Source = @"
            using Microsoft.Extensions.Logging;

            namespace Test
            {
                class MyClass
                {
                    [TagName(""weird.\""na\\me"")]
                    public string? Property { get; set; }

                    [TagName(""provided.\""na\\me"")]
                    [TagProvider(typeof(Provider), nameof(Provider.Provide))]
                    public PropertyToProvide? PropertyToProvide { get; set; }
                }

                class PropertyToProvide
                {
                    public string? Value { get; set; }
                }

                static class Provider
                {
                    public static void Provide(ITagCollector collector, PropertyToProvide? p)
                    {
                    }
                }

                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                    static partial void M(ILogger logger, [LogProperties] MyClass p1);
                }
            }";

        // runs the generator over a full compilation, so the emitted code is verified to actually compile:
        // tag names containing quotes or backslashes would produce uncompilable code if left unescaped
        var generatedSource = RunGeneratorAndAssertNoErrors(Source);

        Assert.Contains("new(\"p1.weird.\\\"na\\\\me\",", generatedSource);
        Assert.Contains("state.TagNamePrefix = \"p1.provided.\\\"na\\\\me\";", generatedSource);
    }

    [Fact]
    public void TagProviderOnTransitivelyNestedProperty()
    {
        const string Source = @"
            using Microsoft.Extensions.Logging;

            namespace Test
            {
                class MyClass
                {
                    [LogProperties]
                    public Nested? Nested { get; set; }
                }

                class Nested
                {
                    [TagProvider(typeof(Provider), nameof(Provider.Provide))]
                    public PropertyToProvide? Leaf { get; set; }

                    [TagProvider(typeof(Provider), nameof(Provider.Provide), OmitReferenceName = true)]
                    public PropertyToProvide? OmittedLeaf { get; set; }
                }

                class PropertyToProvide
                {
                    public string? Value { get; set; }
                }

                static class Provider
                {
                    public static void Provide(ITagCollector collector, PropertyToProvide? p)
                    {
                    }
                }

                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                    static partial void M(ILogger logger, [LogProperties] MyClass p1);

                    [LoggerMessage(1, LogLevel.Debug, ""Parameter"")]
                    static partial void M2(ILogger logger, [LogProperties(OmitReferenceName = true)] MyClass p2);
                }
            }";

        var generatedSource = RunGeneratorAndAssertNoErrors(Source);

        // parameter name and leaf name are both included
        Assert.Contains("state.TagNamePrefix = \"p1.Nested.Leaf\";", generatedSource);

        // parameter name included, leaf name omitted
        Assert.Contains("state.TagNamePrefix = \"p1.Nested\";", generatedSource);

        // parameter name omitted, leaf name included
        Assert.Contains("state.TagNamePrefix = \"Nested.Leaf\";", generatedSource);

        // parameter name and leaf name are both omitted
        Assert.Contains("state.TagNamePrefix = \"Nested\";", generatedSource);
    }

    // Runs the generator over a full compilation so that the emitted code is verified to actually compile.
    private static string RunGeneratorAndAssertNoErrors(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new LoggingGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            CompilationHelper.CreateCompilation(source),
            out var outputCompilation,
            out var generatorDiagnostics);

        Assert.Empty(generatorDiagnostics);
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        var generatedSource = Assert.Single(driver.GetRunResult().Results[0].GeneratedSources);
        return generatedSource.SourceText.ToString();
    }
}
