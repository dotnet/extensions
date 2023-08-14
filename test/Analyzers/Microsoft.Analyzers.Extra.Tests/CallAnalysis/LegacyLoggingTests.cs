// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static partial class LegacyLoggingTests
{
    [Fact]
    public static async Task TargetClassInNamespace()
    {
        const string OriginalTarget = @"
                namespace Example
                {
                    public static partial class Log
                    {
                    }
                }
                ";

        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                        }
                    }
                }
                ";
        const string ExpectedTarget = @"
                namespace Example
                {
                    public static partial class Log
                    {

        [Microsoft.Extensions.Logging.LoggerMessage(0, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello"")]
        internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);
    }
                }
                ";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource, OriginalTarget },
            defaultNamespace: "Example").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task UsesExistingParameterNamesAsArgs()
    {
        const string OriginalTarget = @"
                namespace Example
                {
                    public static partial class Log
                    {
                    }
                }
                ";

        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class MyObject
                    {
                        public string MyProperty { get; set; } = ""This is a test"";
                    }

                    public class TestClass
                    {
                        private ILogger logger;
                        public TestClass(ILogger logger)
                        {
                            this.logger = logger;
                        }

                        public void Test()
                        {
                            var myObject = new MyObject();
                            var logger = new Logger<TestClass>(this.logger);
                            this.logger.LogTrace($""Hello {myObject.MyProperty}"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class MyObject
                    {
                        public string MyProperty { get; set; } = ""This is a test"";
                    }

                    public class TestClass
                    {
                        private ILogger logger;
                        public TestClass(ILogger logger)
                        {
                            this.logger = logger;
                        }

                        public void Test()
                        {
                            var myObject = new MyObject();
                            var logger = new Logger<TestClass>(this.logger);
                            this.logger.HelloMyProperty(myObject.MyProperty);
                        }
                    }
                }
                ";
        const string ExpectedTarget = @"
                namespace Example
                {
                    public static partial class Log
                    {

        [Microsoft.Extensions.Logging.LoggerMessage(0, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello {myProperty}"")]
        internal static partial void HelloMyProperty(this Microsoft.Extensions.Logging.ILogger logger, string myProperty);
    }
                }
                ";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource, OriginalTarget },
            defaultNamespace: "Example").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task TargetClassDoesntExistWithNamespace()
    {
        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                        }
                    }
                }
                ";
        const string ExpectedTarget = @"
namespace Example
{
#pragma warning disable CS8019
    using Microsoft.Extensions.Logging;
    using System;
#pragma warning restore CS8019

    static partial class Log
    {

        [LoggerMessage(0, LogLevel.Trace, ""Hello"")]
        internal static partial void Hello(this ILogger logger);
    }
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource },
            extraFile: "Log.cs",
            defaultNamespace: "Example").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task TargetClassDoesntExistWithFileScopedNamespace()
    {
        const string OriginalTarget = @"
            namespace Example;

            public static partial class Log
            {
            }";

        const string OriginalSource = @"
            using Microsoft.Extensions.Logging;

            namespace Example;

            public class TestClass
            {
                public void Test(ILogger logger)
                {
                    logger.LogTrace(""Hello"");
                }
            }";

        const string ExpectedSource = @"
            using Microsoft.Extensions.Logging;

            namespace Example;

            public class TestClass
            {
                public void Test(ILogger logger)
                {
                    logger.Hello();
                }
            }";

        const string ExpectedTarget = @"
            namespace Example;

            public static partial class Log
            {

    [Microsoft.Extensions.Logging.LoggerMessage(0, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);
}";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource, OriginalTarget },
            defaultNamespace: "Example").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task TargetClassExistWithDeepNamespace()
    {
        const string OriginalTarget = @"
                namespace Example
                {
                    namespace Example2
                    {
                        static partial class Log
                        {
                        }
                    }
                }
                ";

        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                        }
                    }
                }
                ";

        const string ExpectedTarget = @"
                namespace Example
                {
                    namespace Example2
                    {
                        static partial class Log
                        {

            [Microsoft.Extensions.Logging.LoggerMessage(0, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello"")]
            internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);
        }
                    }
                }
                ";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource, OriginalTarget },
            defaultNamespace: "Example.Example2").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task TargetClassDoesntExistInNestedType()
    {
        const string OriginalTarget = @"
                namespace Example
                {
                    class Container
                    {
                        static partial class Log
                        {
                        }
                    }
                }
                ";

        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                        }
                    }
                }
                ";

        const string ExpectedTarget = @"
namespace Example.Example2
{
#pragma warning disable CS8019
    using Microsoft.Extensions.Logging;
    using System;
#pragma warning restore CS8019

    static partial class Log
    {

        [LoggerMessage(0, LogLevel.Trace, ""Hello"")]
        internal static partial void Hello(this ILogger logger);
    }
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource, OriginalTarget },
            extraFile: "Log.cs",
            defaultNamespace: "Example.Example2").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[2];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task DuplicateFilename()
    {
        const string OriginalTarget = @"
                namespace Example
                {
                }
                ";

        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                        }
                    }
                }
                ";

        const string ExpectedTarget = @"
namespace Example
{
#pragma warning disable CS8019
    using Microsoft.Extensions.Logging;
    using System;
#pragma warning restore CS8019

    static partial class Log
    {

        [LoggerMessage(0, LogLevel.Trace, ""Hello"")]
        internal static partial void Hello(this ILogger logger);
    }
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource, OriginalTarget },
            sourceNames: new[] { "primary.cs", "Log.cs" },
            extraFile: "Log2.cs",
            defaultNamespace: "Example").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[2];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task MissingMetadata()
    {
        const string OriginalTarget = @"
                namespace Example
                {
                    public static partial class Log
                    {
                    }
                }
                ";

        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                        }
                    }
                }
                ";

        const string ExpectedTarget = @"
                namespace Example
                {
                    public static partial class Log
                    {

        [Microsoft.Extensions.Logging.LoggerMessage(1, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello"")]
        internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);
    }
                }
                ";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer
            {
                GetTypeByMetadataName3 = (_, _) => null,
            },
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! },
            new[] { OriginalSource, OriginalTarget },
            defaultNamespace: "Example").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static void UtilityMethods()
    {
        var f = new LegacyLoggingFixer();
        Assert.Single(f.FixableDiagnosticIds);
        Assert.Equal(DiagDescriptors.LegacyLogging.Id, f.FixableDiagnosticIds[0]);
        Assert.Null(f.GetFixAllProvider());
    }

    [Fact]
    public static async Task FailureModes()
    {
        const string TargetSourceCode = @"
                using Microsoft.Extensions.Logging;
                using System;
                using System.Runtime.CompilerServices;

                namespace Example
                {
                    namespace Example2
                    {
                        /*0+*/static partial class Log/*-0*/
                        {
                            [Microsoft.Extensions.Telemetry.LoggerMessage(0, LogLevel.Trace, ""Hello"")]
                            internal static void Hello(ILogger logger) {}

                            [Obsolete]
                            [MethodImpl(MethodImplOptions.AggressiveInlining)]
                            internal static void World() {}
                        }
                    }
                }
                ";

        const string InvocationSourceCode = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public static void TestMethod(ILogger logger)
                        {
                            /*0+*/logger.LogTrace(""TestA"")/*-0*/;
                        }
                    }
                }
                ";

        var proj = RoslynTestUtils
            .CreateTestProject(new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LoggerMessageAttribute))! })
                .WithDocument("target.cs", TargetSourceCode)
                .WithDocument("invocation.cs", InvocationSourceCode);

        proj.CommitChanges();
        var targetDoc = proj.FindDocument("target.cs");
        var targetRoot = await targetDoc.GetSyntaxRootAsync(CancellationToken.None).ConfigureAwait(false);
        var targetClass = targetRoot!.FindNode(RoslynTestUtils.MakeTextSpan(TargetSourceCode, 0)) as ClassDeclarationSyntax;
        var invocationDoc = proj.FindDocument("invocation.cs");

        // make sure this works normally
        var f = new LegacyLoggingFixer();
        var (invocationExpression, details) = await f.CheckIfCanFixAsync(
            invocationDoc,
            InvocationSourceCode.MakeTextSpan(0),
            CancellationToken.None).ConfigureAwait(false);

        Assert.NotNull(invocationExpression);
        Assert.NotNull(details);

        var (methodName, existing) = await f.GetFinalTargetMethodNameAsync(
            targetDoc,
            targetClass!,
            invocationDoc,
            invocationExpression!,
            details!,
            CancellationToken.None).ConfigureAwait(false);

        Assert.Equal("TestA", methodName);
        Assert.False(existing);

        f = new LegacyLoggingFixer
        {
            GetSyntaxRootAsync = (_, _) => Task.FromResult<SyntaxNode?>(null)
        };

        (invocationExpression, details) = await f.CheckIfCanFixAsync(
            invocationDoc,
            RoslynTestUtils.MakeTextSpan(InvocationSourceCode, 0),
            CancellationToken.None).ConfigureAwait(false);

        Assert.Null(invocationExpression);
        Assert.Null(details);

        f = new LegacyLoggingFixer
        {
            GetSemanticModelAsync = (_, _) => Task.FromResult<SemanticModel?>(null)
        };

        (invocationExpression, details) = await f.CheckIfCanFixAsync(
            invocationDoc,
            RoslynTestUtils.MakeTextSpan(InvocationSourceCode, 0),
            CancellationToken.None).ConfigureAwait(false);

        Assert.Null(invocationExpression);
        Assert.Null(details);

        f = new LegacyLoggingFixer
        {
            GetOperation = (_, _, _) => null
        };

        (invocationExpression, details) = await f.CheckIfCanFixAsync(
            invocationDoc,
            RoslynTestUtils.MakeTextSpan(InvocationSourceCode, 0),
            CancellationToken.None).ConfigureAwait(false);

        Assert.Null(invocationExpression);
        Assert.Null(details);

        f = new LegacyLoggingFixer
        {
            GetDeclaredSymbol = (_, _, _) => null
        };

        (invocationExpression, details) = await f.CheckIfCanFixAsync(
            invocationDoc,
            RoslynTestUtils.MakeTextSpan(InvocationSourceCode, 0),
            CancellationToken.None).ConfigureAwait(false);

        Assert.NotNull(invocationExpression);
        Assert.NotNull(details);

        (methodName, existing) = await f.GetFinalTargetMethodNameAsync(
            targetDoc,
            targetClass!,
            invocationDoc,
            invocationExpression!,
            details!,
            CancellationToken.None).ConfigureAwait(false);

        Assert.Equal("TestA", methodName);
        Assert.False(existing);

        f = new LegacyLoggingFixer
        {
            GetTypeByMetadataName1 = (_, _) => null
        };

        (invocationExpression, details) = await f.CheckIfCanFixAsync(
            invocationDoc,
            RoslynTestUtils.MakeTextSpan(InvocationSourceCode, 0),
            CancellationToken.None).ConfigureAwait(false);

        Assert.Null(invocationExpression);
        Assert.Null(details);

        f = new LegacyLoggingFixer
        {
            GetTypeByMetadataName2 = (_, _) => null
        };

        (invocationExpression, details) = await f.CheckIfCanFixAsync(
            invocationDoc,
            RoslynTestUtils.MakeTextSpan(InvocationSourceCode, 0),
            CancellationToken.None).ConfigureAwait(false);

        Assert.NotNull(invocationExpression);
        Assert.NotNull(details);

        (methodName, existing) = await f.GetFinalTargetMethodNameAsync(
            targetDoc,
            targetClass!,
            invocationDoc,
            invocationExpression!,
            details!,
            CancellationToken.None).ConfigureAwait(false);

        Assert.Equal("TestA", methodName);
        Assert.False(existing);
    }
}
