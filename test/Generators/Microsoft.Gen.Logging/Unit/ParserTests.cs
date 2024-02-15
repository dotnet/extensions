// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
using Xunit;

namespace Microsoft.Gen.Logging.Test;

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
    public async Task NullableLogger()
    {
        await RunGenerator(@"
            namespace TestClasses
            {
                internal static partial class NullableTestExtensions
                {
                    [LoggerMessage(6, LogLevel.Debug, ""M6 {p0}"")]
                    internal static partial void M6(ILogger? logger, string p0);
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
    public async Task InvalidMethodBody()
    {
        const string Source = @"
                partial class C
                {
                    static partial void M1(ILogger logger);

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger)
                    /*0+*/{
                    }/*-0*/
                }
            ";

        await RunGenerator(Source, DiagDescriptors.LoggingMethodHasBody);
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

        await RunGenerator(source, DiagDescriptors.TemplateStartsWithAtSymbol);
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
    public async Task MethodReturnType()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial /*0+*/int/*-0*/ M1(ILogger logger);

                    public static partial int M1(ILogger logger) { return 0; }
                }
            ";

        await RunGenerator(Source, DiagDescriptors.LoggingMethodMustReturnVoid);
    }

    [Fact]
    public async Task MissingILogger()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1 {p1}"")]
                    static partial void M1/*0+*/(int p1)/*-0*/;
                }
            ";

        await RunGenerator(Source, DiagDescriptors.MissingLoggerParameter);
    }

    [Fact]
    public async Task NotStatic()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    partial void /*0+*/M1/*-0*/(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.LoggingMethodShouldBeStatic);
    }

    [Fact]
    public async Task NoILoggerField()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void /*0+*/M1/*-0*/();
                }
            ";

        await RunGenerator(Source, DiagDescriptors.MissingLoggerMember);
    }

    [Fact]
    public async Task MultipleILoggerMembers()
    {
        const string Source = @"
                partial class C
                {
                    public ILogger _logger1;
                    public ILogger /*0+*/_logger2/*-0*/;

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void M1();
                }

                partial class D
                {
                    public ILogger? Logger { get; set; } = null!;
                    public ILogger /*1+*/_logger2/*-1*/;

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void M1();
                }

                partial class E
                {
                    public ILogger<int> Logger { get; set; } = null!;
                    public ILogger /*2+*/_logger2/*-2*/;

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void M1();
                }

                partial class F
                {
                    public ILogger? /*3+*/Logger/*-3*/ { get; set; }

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void M1();
                }

                partial class G : F
                {
                    private readonly ILogger? _logger;

                    [LoggerMessage(1, LogLevel.Debug, ""M2"")]
                    public partial void M2();
                }
            ";

        await RunGenerator(Source, DiagDescriptors.MultipleLoggerMembers);
    }

    [Fact]
    public async Task InstanceEmptyLoggingMethod()
    {
        const string Source = @"
            partial class C
            {
                public ILogger _logger;

                [LoggerMessage]
                public partial void /*0+*/M1/*-0*/(LogLevel level);

                [LoggerMessage(LogLevel.Debug)]
                public partial void /*1+*/M2/*-1*/();
            }";

        await RunGenerator(Source, DiagDescriptors.EmptyLoggingMethod);
    }

    [Fact]
    public async Task StaticEmptyLoggingMethod()
    {
        const string Source = @"
            partial class C
            {
                [LoggerMessage]
                public static partial void /*0+*/M1/*-0*/(ILogger logger, LogLevel level);

                [LoggerMessage(LogLevel.Debug)]
                public static partial void /*1+*/M2/*-1*/(ILogger logger);
            }";

        await RunGenerator(Source, DiagDescriptors.EmptyLoggingMethod);
    }

    [Fact]
    public async Task NonEmptyLoggingMethod()
    {
        await RunGenerator(@"
                partial class C
                {
                    public ILogger _logger;

                    [LoggerMessage]
                    public partial void M1(LogLevel level, Exception ex);

                    [LoggerMessage(LogLevel.Debug)]
                    public partial void M2(Exception ex);

                    [LoggerMessage]
                    public static partial void M3(ILogger logger, LogLevel level, Exception ex);

                    [LoggerMessage(LogLevel.Debug)]
                    public static partial void M4(ILogger logger, Exception ex);
                }");
    }

    [Fact]
    public async Task NotPartial()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static void /*0+*/M1/*-0*/(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.LoggingMethodMustBePartial);
    }

    [Fact]
    public async Task MethodGeneric()
    {
        const string Source = @"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1/*0+*/<T>/*-0*/(ILogger logger);
                }
            ";

        await RunGenerator(Source, DiagDescriptors.LoggingMethodIsGeneric);
    }

    [Theory]
    [CombinatorialData]
    public async Task LogMethodParamsRefKind([CombinatorialValues("ref", "out")] string modifier)
    {
        string source = @$"
            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""Parameter {{P1}}"")]
                static partial void M(ILogger logger, {modifier} int /*0+*/p1/*-0*/);
            }}";

        await RunGenerator(source, DiagDescriptors.LoggingMethodParameterRefKind);
    }

    [Theory]
    [CombinatorialData]
    public async Task LogMethod_DetectsSensitiveMembersInRecord([CombinatorialRange(0, TotalSensitiveCases)] int positionNumber)
    {
        var sb = new System.Text.StringBuilder(@"
            using Microsoft.Extensions.Compliance.Testing;
            using System.Collections;

            {17}
            public record class BaseRecord
            {
                {0}public string PropBase { get; }
                {1}public string FieldBase;
            }

            {18}
            public record struct InlineStruct({2}string InlineProp);

            {19}
            public record class InterimRecord : BaseRecord
            {
                {3}public string PropInterim { get; }
                {4}public string FieldInterim;

                // Hiding on purpose:
                {5}public new string FieldBase;
                public virtual string PropVirt => nameof(PropVirt);
            }

            {20}
            // 'internal' on purpose
            internal record struct EnumerableStructRecord : IEnumerable
            {
                {6}public string EnumProp => nameof(EnumProp);
                public IEnumerator GetEnumerator() => null!;
            }

            {16}
            record MyRecord : InterimRecord
            {
                {7}public string Field;
                {8}public string PropGet { get; }
                {9}public string PropGetSet { get; set; }
                {10}public string PropPrivateGet { private get; set; }
                {11}public string PropInternalGet { internal get; set; }
                {12}public string PropProtectedGet { protected get; set; }
                {13}public override string PropVirt => nameof(MyRecord);
                {14}public string PropGetInit { get; init; }
                public EnumerableStructRecord PropRecord { get; } = new();
                {15}public InlineStruct FieldRecord = new(nameof(FieldRecord));
            }

            partial class C
            {
                [LoggerMessage(LogLevel.Debug, ""Param is {p0}"")]
                public static partial void LogTemplate(ILogger logger, IRedactorProvider provider, MyRecord /*0+*/p0/*-0*/);

                [LoggerMessage(LogLevel.Debug, ""No param here"")]
                public static partial void LogStructured(ILogger logger, MyRecord /*1+*/p0/*-1*/);

                [LoggerMessage(LogLevel.Debug)]
                public static partial void LogFullyStructured(ILogger logger, MyRecord /*2+*/p0/*-2*/);

                [LoggerMessage(LogLevel.Debug, ""Param is {p0}"")]
                public static partial void LogProperties(ILogger logger, IRedactorProvider rp, [LogProperties] MyRecord /*3+*/p0/*-3*/);
            }");

        for (int i = 0; i < TotalSensitiveCases; i++)
        {
            var template = "{" + i.ToString() + "}";
            var replacement = i switch
            {
                _ when positionNumber == i
                        => "[PrivateData] ",
                _ => string.Empty,
            };

            sb.Replace(template, replacement);
        }

        await RunGenerator(
            sb.ToString(),
            DiagDescriptors.RecordTypeSensitiveArgumentIsInTemplate,
            ignoreDiag: DiagDescriptors.ParameterHasNoCorrespondingTemplate);
    }

    [Theory]
    [InlineData("System.Nullable<int>")]
    [InlineData("int?")]
    public async Task LogMethod_DetectsSensitiveNullableMembersInRecord(string type)
    {
        await RunGenerator(@$"
            using Microsoft.Extensions.Compliance.Testing;

            record MyRecord
            {{
                [PrivateData]
                public {type} Field;
            }}

            partial class C
            {{
                [LoggerMessage(LogLevel.Debug, ""Param is {{p0}}"")]
                public static partial void LogTemplate(ILogger logger, MyRecord /*0+*/p0/*-0*/);
            }}",
            DiagDescriptors.RecordTypeSensitiveArgumentIsInTemplate);
    }

    [Theory]
    [InlineData("[PrivateData]")]
    [InlineData("[property: PrivateData]")]
    public async Task LogMethod_DetectsSensitiveMembersInRecord_WithPrimaryCtor(string annotation)
    {
        await RunGenerator(@$"
            using Microsoft.Extensions.Compliance.Testing;

            record MyRecord({annotation} string userIp);

            partial class C
            {{
                [LoggerMessage(LogLevel.Debug, ""Param is {{p0}}"")]
                public static partial void LogTemplate(ILogger logger, MyRecord /*0+*/p0/*-0*/);
            }}",
            DiagDescriptors.RecordTypeSensitiveArgumentIsInTemplate);
    }

    [Fact]
    public async Task LogMethod_SkipsNonSensitiveMembersInRecord()
    {
        await RunGenerator(@"
            using Microsoft.Extensions.Compliance.Testing;

            public record class BaseRecord
            {
                [PrivateData] public const int ConstFieldBase = 99;
                [PrivateData] public static int StaticFieldBase;
                [PrivateData] public static int StaticPropBase => 100;
                [PrivateData] private int PrivateFieldBase;
                [PrivateData] internal int InternalFieldBase;
            }

            public record class InterimRecord : BaseRecord
            {
                [PrivateData] public virtual decimal PropVirt => decimal.MinusOne;
            }

            internal record class MyRecord : InterimRecord
            {
                [PrivateData] public const int ConstField = 99;
                [PrivateData] public static int StaticField;
                [PrivateData] public static int StaticProp => 100;
                [PrivateData] private int PrivateField;
                [PrivateData] internal int InternalField;
                [PrivateData] private string PrivatePropGetSet { get; set; }
                [PrivateData] internal string InternalPropGetSet { get; set; }
                [PrivateData] protected string ProtectedPropGetSet { get; set; }

                // This one overrides 'virtual' property declared in 'InterimRecord':
                public override decimal PropVirt => decimal.One;
            }

            partial class C
            {
                [LoggerMessage(LogLevel.Debug, ""Param is {p0}"")]
                public static partial void LogFunc(ILogger logger, IRedactorProvider provider, MyRecord /*0+*/p0/*-0*/);
            }");
    }

    [Fact]
    public async Task Templates()
    {
        await RunGenerator(@"
                partial class C
                {
                    [LoggerMessage(1, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(2, LogLevel.Debug, ""M2 {arg1} {arg2}"")]
                    static partial void M2(ILogger logger, string arg1, string arg2);

                    [LoggerMessage(3, LogLevel.Debug, ""M3 {arg1"")]
                    static partial void M3(ILogger logger);

                    [LoggerMessage(4, LogLevel.Debug, ""M4 arg1}"")]
                    static partial void M4(ILogger logger);

                    [LoggerMessage(5, LogLevel.Debug, ""M5 {"")]
                    static partial void M5(ILogger logger);

                    [LoggerMessage(6, LogLevel.Debug, ""}M6 "")]
                    static partial void M6(ILogger logger);

                    [LoggerMessage(7, LogLevel.Debug, ""M7 {{arg1}}"")]
                    static partial void M7(ILogger logger);
                }
            ");
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

        var (d, _) = await RoslynTestUtils.RunGenerator(
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
