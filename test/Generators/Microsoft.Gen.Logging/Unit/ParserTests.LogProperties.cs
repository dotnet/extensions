// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Gen.Logging.Parsing;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public partial class ParserTests
{
    [Fact]
    public static async Task InvalidLogPropertiesUsage()
    {
        await RunGenerator(@"
            class MyClass2
            {
                public int A { get; set; }
            }   

            class MyClass
            {
                [/*0+*/LogProperties/*-0*/]
                internal MyClass2 P0 { get; set; }

                [/*1+*/LogProperties/*-1*/]
                internal static MyClass2 P1 { get; set; }

                [/*2+*/LogProperties/*-2*/]
                internal MyClass2 P2 { set; }

                [/*3+*/LogProperties/*-3*/]
                public MyClass2 P3 { internal get; set; }

                public int A { get; set; }
            }

            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""Parameter"")]
                static partial void M0(ILogger logger, [LogProperties] MyClass p1);
            }", DiagDescriptors.InvalidAttributeUsage);
    }

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
                [LoggerMessage(0, LogLevel.Debug, ""Parameterless..."")]
                static partial void M0(ILogger logger, [LogProperties(OmitReferenceName = true)] MyType /*0+*/p0/*-0*/);
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
                [LoggerMessage]
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
                [LoggerMessage(LogLevel.Debug)]
                public static partial void LogFunc(ILogger logger, [LogProperties] MyRecord p0);
            }");
    }

    [Theory]
    [InlineData("LogLevel")]
    [InlineData("System.Exception")]
    public async Task LogPropertiesInvalidUsage(string annotation)
    {
        // We don't check [LogProperties] on ILogger here since it produces a lot of errors
        string source = @$"
            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""Parameterless..."")]
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
                [LoggerMessage(0, LogLevel.Debug, ""Parameter {{P1}}"")]
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
                [LoggerMessage(0, LogLevel.Debug, ""Empty template"")]
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
                [LoggerMessage(0, LogLevel.Debug, ""{Param} is here"")]
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
                [LoggerMessage(0, LogLevel.Debug, ""{param}"")]
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
                [LoggerMessage(0, LogLevel.Debug, ""{Param} {Param}"")]
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
                [LoggerMessage(0, LogLevel.Debug, ""{param_A}"")]
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

                [LogProperties]
                public MyTransitiveClass Transitive { get; set; }
            }

            public class MyTransitiveClass
            {
                public int Prop { get; set; }
            }

            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties] MyClass /*0+*/param/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesNameCollision);
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
            class MyClass
            {{
                [LogProperties]
                public {type} /*0+*/Prop/*-0*/ {{ get; set; }}
            }}

            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""No params..."")]
                static partial void M0(ILogger logger, [LogProperties] {type} /*1+*/test/*-1*/);

                [LoggerMessage(1, LogLevel.Debug, ""No params..."")]
                static partial void M1(ILogger logger, [LogProperties] MyClass test);
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
                [LogProperties]
                public {propertyType} Prop {{ get; set; }}
            }}

            public class ClassB
            {{
                [LogProperties]
                public ClassC Prop {{ get; set; }}
            }}

            public class ClassC
            {{
                [LogProperties]
                public ClassA Prop {{ get; set; }}
            }}

            public struct StructA
            {{
                [LogProperties]
                public ClassA Prop {{ get; set; }}
            }}

            partial class LoggerClass
            {{
                [LoggerMessage(0, LogLevel.Debug, ""No params..."")]
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
                [LoggerMessage(0, LogLevel.Debug, ""No params..."")]
                static partial void M(ILogger logger, [LogProperties] DerivedType /*0+*/test/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.LogPropertiesHiddenPropertyDetected);
    }
}
