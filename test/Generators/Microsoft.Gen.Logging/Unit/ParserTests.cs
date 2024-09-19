// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Logging;
using Microsoft.Gen.Logging.Parsing;
using Microsoft.Gen.Shared;
using VerifyXunit;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

[UsesVerify]
public partial class ParserTests
{
    private const int TotalSensitiveCases = 21;

    [Fact]
    public async Task IncompatibleAttributes()
    {
        await RunGenerator(@$"
            namespace TestClasses
            {{
                public partial class LoggerInPropertyTestClass
                {{
                    public ILogger Logger {{ get; set; }} = null!;

                    [LoggerMessage(0, LogLevel.Debug, ""M0 {{p0}}"")]
                    public partial void M0(string p0);
                }}

                public partial class LoggerInNullablePropertyTestClass
                {{
                    public ILogger? Logger {{ get; set; }}

                    [LoggerMessage(0, LogLevel.Debug, ""M0 {{p0}}"")]
                    public partial void M0(string p0);
                }}

                public partial class GenericLoggerInPropertyTestClass
                {{
                    public ILogger<int> Logger {{ get; set; }} = null!;

                    [LoggerMessage(0, LogLevel.Debug, ""M0 {{p0}}"")]
                    public partial void M0(string p0);
                }}

                public partial class LoggerInPropertyDerivedTestClass : LoggerInPropertyTestClass
                {{
                    [LoggerMessage(1, LogLevel.Debug, ""M1 {{p0}}"")]
                    public partial void M1(string p0);
                }}

                public partial class LoggerInNullablePropertyDerivedTestClass : LoggerInNullablePropertyTestClass
                {{
                    [LoggerMessage(1, LogLevel.Debug, ""M1 {{p0}}"")]
                    public partial void M1(string p0);
                }}

                public partial class GenericLoggerInPropertyDerivedTestClass : LoggerInNullablePropertyTestClass
                {{
                    [LoggerMessage(1, LogLevel.Debug, ""M1 {{p0}}"")]
                    public partial void M1(string p0);
                }}
            }}
        ");
    }

    [Fact]
    public async Task NullableStructEnumerable()
    {
        await RunGenerator(@"
            using System.Collections.Generic;

            namespace TestClasses
            {
                public readonly struct StructEnumerable : IEnumerable<int>
                {
                    private static readonly List<int> _numbers = new() { 1, 2, 3 };
                    public IEnumerator<int> GetEnumerator() => _numbers.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _numbers.GetEnumerator();
                }

                internal static partial class NullableTestExtensions
                {
                    /// <summary>
                    /// A comment!
                    /// </summary>
                    [LoggerMessage(13, LogLevel.Error, ""M13{p1}"")]
                    public static partial void M13(ILogger logger, StructEnumerable p1);

                    [LoggerMessage(14, LogLevel.Error, ""M14{p1}"")]
                    public static partial void M14(ILogger logger, StructEnumerable? p1);
                }
            }");
    }

    [Fact]
    public async Task MissingAttributeValue()
    {
        await RunGenerator(@"
                internal static partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M0 {p0}"", EventName = )]
                    static partial void M0(ILogger logger, string p0);

                    [LoggerMessage(1, LogLevel.Debug, ""M0 {p0}"", SkipEnabledChecks = )]
                    static partial void M1(ILogger logger, string p0);
                }
            ");
    }

    [Fact]
    public async Task WithNullLevel_GeneratorWontFail()
    {
        await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, null, ""This is a message with {foo}"")]
                    static partial void M1(ILogger logger, string foo);
                }
            ");
    }

    [Fact]
    public async Task WithNullEventId_GeneratorWontFail()
    {
        await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(null, LogLevel.Debug, ""This is a message with {foo}"")]
                    static partial void M1(ILogger logger, string foo);
                }
            ");
    }

    [Fact]
    public async Task WithNullMessage_GeneratorWontFail()
    {
        await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, null)]
                    static partial void M1(ILogger logger, string foo);
                }
            ");
    }

    [Fact]
    public async Task ParameterlessConstructor()
    {
        await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage()]
                    static partial void M1(ILogger logger, LogLevel level, string foo);

                    [LoggerMessage]
                    static partial void M2(ILogger logger, LogLevel level, string foo);

                    [LoggerMessage(SkipEnabledChecks = true)]
                    static partial void M3(ILogger logger, LogLevel level, string foo);
                }");
    }

    [Fact]
    public async Task UnderscoresInMethodName()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void __M1(ILogger logger);
                }
            ";

        await RunGenerator(Source);
    }

    [Fact]
    public async Task MissingLogLevel()
    {
        const string Source = @"
                partial class C
                {
                    /*0+*/[LoggerMessage(""M1"")]
                    static partial void M1(ILogger logger);/*-0*/
               }
            ";

        await RunGenerator(Source, DiagDescriptors.MissingLogLevel);
    }

    [Fact]
    public async Task MissingLogLevel_WhenDefaultCtor()
    {
        const string Source = @"
                partial class C
                {
                    /*0+*/[LoggerMessage]
                    static partial void M1(ILogger logger);/*-0*/
                }
            ";

        await RunGenerator(Source, DiagDescriptors.MissingLogLevel);
    }

    [Fact]
    public async Task MissingTemplate()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""This is a message without foo"")]
                    static partial void M1(ILogger logger, string /*0+*/foo/*-0*/);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.ParameterHasNoCorrespondingTemplate);
    }

    [Fact]
    public async Task MissingArgument()
    {
        const string Source = @"
                partial class C
                {
                    [/*0+*/LoggerMessage(0, LogLevel.Debug, ""{foo}"")/*-0*/]
                    static partial void M1(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.TemplateHasNoCorrespondingParameter);
    }

    [Theory]
    [InlineData("{@param}")]
    [InlineData("{@param,10}")]
    [InlineData("{@param,10:5}")]
    [InlineData("{@param:5}")]
    [InlineData("{@param }")]
    [InlineData("{ @param}")]
    [InlineData("{ @param }")]
    [InlineData("{ @param:10 }")]
    [InlineData("{ @param : 10 }")]
    [InlineData("{ @param, 10 }")]
    [InlineData("{ @param , 10 }")]
    [InlineData("{ @param , 10 : 5 }")]
    [InlineData(" Beginning ... {  @param  } ending ")]
    [InlineData(" Beginning ... {  @param , 10 : 5 } ending ")]
    public async Task AtSymbolInTemplate(string template)
    {
        string source = @$"
            partial class C
            {{
                [/*0+*/LoggerMessage(LogLevel.Debug, ""{template}"")/*-0*/]
                static partial void M1(ILogger logger, int param);
            }}";

        await RunGenerator(source);
    }

    [Theory]
    [InlineData("{request}", "request")]
    [InlineData("{request}", "@request")]
    [InlineData("{@request}", "request")]
    [InlineData("{@request}", "@request")]
    public async Task AtSymbolArgumentOutOfOrder(string stringTemplate, string parameterName)
    {
        await RunGenerator(@$"
                partial class C
                {{
                    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""{stringTemplate} {{a1}}"")]
                    static partial void M1(ILogger logger,string a1, string {parameterName});
                }}
            ");
    }

    [Fact]
    public async Task NeedlessQualifierInMessage()
    {
        const string Source = @"
                partial class C
                {
                    [/*0+*/LoggerMessage(0, LogLevel.Information, ""INFO: this is an informative message"")/*-0*/]
                    static partial void M1(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.RedundantQualifierInMessage);
    }

    [Fact]
    public async Task NeedlessExceptionInMessage()
    {
        const string Source = @"
                partial class C
                {
                    [/*0+*/LoggerMessage(0, LogLevel.Debug, ""M1 {ex} {ex2}"")/*-0*/]
                    static partial void M1(ILogger logger, System.Exception ex, System.Exception ex2);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.ShouldntMentionExceptionInMessage);
    }

    [Fact]
    public async Task NeedlessLogLevelInMessage()
    {
        const string Source = @"
                partial class C
                {
                    [/*0+*/LoggerMessage(0, ""M1 {l1} {l2}"")/*-0*/]
                    static partial void M1(ILogger logger, LogLevel l1, LogLevel l2);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.ShouldntMentionLogLevelInMessage);
    }

    [Fact]
    public async Task NeedlessLoggerInMessage()
    {
        const string Source = @"
                partial class C
                {
                    [/*0+*/LoggerMessage(0, LogLevel.Debug, ""M1 {logger}"")/*-0*/]
                    static partial void M1(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.ShouldntMentionLoggerInMessage);
    }

    [Fact]
    public async Task FileScopedNamespace()
    {
        await RunGenerator(@"
            namespace Test;
            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""{P1}"")]
                static partial void M1(ILogger logger, int p1);
            }", inNamespace: false);
    }

    [Theory]
    [InlineData("_foo")]
    [InlineData("__foo")]
    [InlineData("@_foo", "_foo")]
    public async Task UnderscoresInParameterName(string name, string? template = null)
    {
        string source = @$"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1 {{{template ?? name}}}"")]
                    static partial void M1(ILogger logger, string {name});
                }}
            ";

        await RunGenerator(source);
    }

    [Fact]
    public async Task MissingExceptionType()
    {
        const string Source = @"
                namespace System
                {
                    public class Object {}
                    public class Void {}
                    public class String {}
                    public struct DateTime {}
                    public class Attribute {}
                }
                namespace System.Collections
                {
                    public interface IEnumerable {}
                }
                namespace Microsoft.Extensions.Logging
                {
                    public enum LogLevel {}
                    public interface ILogger {}
                }
                namespace Microsoft.Extensions.Logging
                {
                    public class LoggerMessageAttribute : System.Attribute {}
                }
                partial class C
                {
                    [Microsoft.Extensions.Logging.LoggerMessageAttribute()]
                    public static partial void Something(this Microsoft.Extensions.Logging.ILogger logger);
                }";

        await RunGenerator(Source, DiagDescriptors.MissingRequiredType, false, includeBaseReferences: false, includeLoggingReferences: false);
    }

    [Fact]
    public async Task MissingEnrichmentPropertyBagTypes()
    {
        const string Source = @"
                namespace Microsoft.Extensions.Logging
                {
                    public enum LogLevel {}
                    public interface ILogger {}
                }
                namespace Microsoft.Extensions.Logging
                {
                    public class LoggerMessageAttribute : System.Attribute {}
                    public class LogPropertiesAttribute : System.Attribute {}
                    public class LogPropertyIgnoreAttribute : System.Attribute {}
                    public class ITagCollector {}
                    public class LogMethodHelper { }
                }
                partial class C
                {
                    [Microsoft.Extensions.Logging.LoggerMessageAttribute]
                    public static partial void Something(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel level, string foo);
                }";

        await RunGenerator(Source, DiagDescriptors.MissingRequiredType, wrap: false, includeLoggingReferences: false);
    }

    [Fact]
    public async Task MissingLoggerMessageAttributeType()
    {
        await RunGenerator(@"
                partial class C
                {
                }
            ", includeLoggingReferences: false);
    }

    [Fact]
    public async Task MissingILoggerType()
    {
        await RunGenerator(@"
                namespace Microsoft.Extensions.Logging
                {
                    public sealed class LoggerMessageAttribute : System.Attribute {}
                }
                partial class C
                {
                }
            ", includeLoggingReferences: false);
    }

    [Fact]
    public async Task MissingLogLevelType()
    {
        await RunGenerator(@"
                namespace Microsoft.Extensions.Logging
                {
                    public sealed class LoggerMessageAttribute : System.Attribute {}
                }
                namespace Microsoft.Extensions.Logging
                {
                    public interface ILogger {}
                }
                partial class C
                {
                }
            ", includeLoggingReferences: false);
    }

    [Fact]
    public async Task EventIdReuse()
    {
        const string Source = @"
                partial class MyClass
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);

                    [/*0+*/LoggerMessage(0, LogLevel.Debug, ""M1"")/*-0*/]
                    static partial void M2(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.ShouldntReuseEventIds);
    }

    [Fact]
    public async Task EventNameReuse()
    {
        const string Source = @"
                partial class MyClass
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"", EventName = ""Dog"")]
                    static partial void M1(ILogger logger);

                    [/*0+*/LoggerMessage(1, LogLevel.Debug, ""M1"", EventName = ""Dog"")/*-0*/]
                    static partial void M2(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.ShouldntReuseEventNames);
    }

    [Fact]
    public async Task ValidTemplates()
    {
        await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = """")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = ""M2"")]
                    static partial void M2(ILogger logger);

                    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = ""{arg1}"")]
                    static partial void M3(ILogger logger, int arg1);

                    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = ""M4 {arg1}"")]
                    static partial void M4(ILogger logger, int arg1);

                    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = ""{arg1} M5"")]
                    static partial void M5(ILogger logger, int arg1);

                    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = ""M6{arg1}M6{arg2}M6"")]
                    static partial void M6(ILogger logger, string arg1, string arg2);

                    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = ""M7 {{const}}"")]
                    static partial void M7(ILogger logger);

                    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = ""{{prefix{{{arg1}}}suffix}}"")]
                    static partial void M8(ILogger logger, string arg1);

                    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = ""prefix }}"")]
                    static partial void M9(ILogger logger);

                    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = ""}}suffix"")]
                    static partial void M10(ILogger logger);
                }
            ");
    }

    [Fact]
    public async Task MalformedFormatString()
    {
        await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1 {A} M1 { M1"")]
                    static partial void /*0+*/M1/*-0*/(ILogger logger);

                    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = ""M2 {A} M2 } M2"")]
                    static partial void /*1+*/M2/*-1*/(ILogger logger);

                    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = ""M3 {arg1"")]
                    static partial void /*2+*/M3/*-2*/(ILogger logger);

                    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = ""M4 arg1}"")]
                    static partial void /*3+*/M4/*-3*/(ILogger logger);

                    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = ""M5 {"")]
                    static partial void /*4+*/M5/*-4*/(ILogger logger);

                    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = ""}M6 "")]
                    static partial void /*5+*/M6/*-5*/(ILogger logger);

                    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = ""{M7{"")]
                    static partial void /*6+*/M7/*-6*/(ILogger logger);

                    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = ""{{{arg1 M8"")]
                    static partial void /*7+*/M8/*-7*/(ILogger logger);

                    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = ""arg1}}} M9"")]
                    static partial void /*8+*/M9/*-8*/(ILogger logger);

                    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = ""{} M10"")]
                    static partial void /*9+*/M10/*-9*/(ILogger logger);

                    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = ""{ } M11"")]
                    static partial void /*10+*/M11/*-10*/(ILogger logger);
                }
            ", DiagDescriptors.MalformedFormatStrings);
    }

    [Fact]
    public async Task Cancellation()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ", cancellationToken: new CancellationToken(true)));
    }

    [Fact]
    public async Task SourceErrors()
    {
        await RunGenerator(@"
                static partial class C
                {
                    // bogus argument type
                    [LoggerMessage(0, "", ""Hello"")]
                    static partial void M1(ILogger logger);

                    // missing parameter name
                    [LoggerMessage(1, LogLevel.Debug, ""Hello"")]
                    static partial void M2(ILogger);

                    // bogus parameter type
                    [LoggerMessage(2, LogLevel.Debug, ""Hello"")]
                    static partial void M3(XILogger logger);

                    // bogus enum value
                    [LoggerMessage(3, LogLevel.Foo, ""Hello"")]
                    static partial void M4(ILogger logger);

                    // attribute applied to something other than a method
                    [LoggerMessage(4, "", ""Hello"")]
                    int M5;
                }
            ");
    }

    [Fact]
    public Task MultipleTypeDefinitions()
    {
        // Adding a dependency to an assembly that has internal definitions of public types
        // should not result in a collision and break generation.
        // Verify usage of the extension GetBestTypeByMetadataName(this Compilation)
        // instead of Compilation.GetTypeByMetadataName().
        var referencedSource = @"
                namespace Microsoft.Extensions.Logging
                {
                    internal class LoggerMessageAttribute { }
                }
                namespace Microsoft.Extensions.Logging
                {
                    internal interface ILogger { }
                    internal enum LogLevel { }
                }";

        // Compile the referenced assembly first.
        Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

        // Obtain the image of the referenced assembly.
        byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

        // Generate the code
        string source = @"
                namespace Test
                {
                    using Microsoft.Extensions.Logging;

                    partial class C
                    {
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }";

        MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

        Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
        LoggingGenerator generator = new LoggingGenerator();

        (IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources) =
            RoslynTestUtils.RunGenerator(compilation, generator);

        // Make sure compilation was successful.
        Assert.Empty(diagnostics);
        Assert.Single(generatedSources);

        return Verifier.Verify(generatedSources[0].SourceText.ToString())
            .AddScrubber(_ => _.Replace(GeneratorUtilities.CurrentVersion, "VERSION"))
            .UseDirectory(Path.Combine("..", "Verified"));
    }

#pragma warning disable S107 // Methods should not have too many parameters
    private static async Task RunGenerator(
        string code,
        DiagnosticDescriptor? expectedDiagnostic = null,
        bool wrap = true,
        bool inNamespace = true,
        bool includeBaseReferences = true,
        bool includeLoggingReferences = true,
        DiagnosticDescriptor? ignoreDiag = null,
        CancellationToken cancellationToken = default)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        var text = code;
        if (wrap)
        {
            var nspaceStart = "namespace Test {";
            var nspaceEnd = "}";
            if (!inNamespace)
            {
                nspaceStart = "";
                nspaceEnd = "";
            }

            text = $@"
                    {nspaceStart}
                    using Microsoft.Extensions.Logging;
                    {code}
                    {nspaceEnd}";
        }

        Assembly[]? refs = null;
        if (includeLoggingReferences)
        {
            refs = new[]
            {
                Assembly.GetAssembly(typeof(ILogger))!,
                Assembly.GetAssembly(typeof(LoggerMessageAttribute))!,
                Assembly.GetAssembly(typeof(IEnrichmentTagCollector))!,
                Assembly.GetAssembly(typeof(DataClassification))!,
                Assembly.GetAssembly(typeof(PrivateDataAttribute))!,
                Assembly.GetAssembly(typeof(BigInteger))!,
            };
        }

        var (d, sources) = await RoslynTestUtils.RunGenerator(
            new LoggingGenerator(),
            refs,
            new[] { text },
            includeBaseReferences: includeBaseReferences,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (ignoreDiag != null)
        {
            d = d.FilterOutDiagnostics(ignoreDiag);
        }

        if (expectedDiagnostic != null)
        {
            RoslynTestUtils.AssertDiagnostics(text, expectedDiagnostic, d);
        }
        else if (d.Count > 0)
        {
            Assert.Fail($"Expected no diagnostics, got {d.Count} diagnostics");
        }
    }
}
