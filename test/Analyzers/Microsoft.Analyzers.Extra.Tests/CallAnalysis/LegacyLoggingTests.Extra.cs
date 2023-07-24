// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static partial class LegacyLoggingTests
{
    [Fact]
    public static async Task PropertyAccess()
    {
        const string OriginalTarget = @"
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{
}
";

        const string OriginalSource = @"
using Microsoft.Extensions.Logging;

namespace Example
{
    public class TestClass
    {
        public class TestOptions
        {
            public string Message { get; set; }
        }
        private TestOptions options = new TestOptions
        {
            Message = ""Test""
        };

        public void Test(ILogger logger)
        {
            logger.LogTrace(""Hello {message}"", options.Message);
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
        public class TestOptions
        {
            public string Message { get; set; }
        }
        private TestOptions options = new TestOptions
        {
            Message = ""Test""
        };

        public void Test(ILogger logger)
        {
            logger.HelloMessage(options.Message);
        }
    }
}
";

        const string ExpectedTarget = @"
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(0, LogLevel.Trace, ""Hello {message}"")]
    internal static partial void HelloMessage(this ILogger logger, string message);
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LogMethodAttribute))! },
            new[] { OriginalSource, OriginalTarget }).ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task MissingReferenceToLogMethodAttribute()
    {
        const string OriginalTarget = @"
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
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
            logger.LogTrace(""Hello World"");
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
            logger.HelloWorld();
        }
    }
}
";

        const string ExpectedTarget = @"
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{

    [Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute(1, LogLevel.Trace, ""Hello World"")]
    internal static partial void HelloWorld(this ILogger logger);
}
";

        // Call the analyzer and fixer without having reference to the Logging.Abstraction assembly, this
        // call should not throw and should generate proper fix.
        string actualSource = string.Empty;
        string actualTarget = string.Empty;

        var exception = await Record.ExceptionAsync(async () =>
        {
            var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))! },
            new[] { OriginalSource, OriginalTarget }).ConfigureAwait(false);

            actualSource = l[0];
            actualTarget = l[1];
        });

        Assert.Null(exception);

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task TargetClassDoesntExist()
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
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(0, LogLevel.Trace, ""Hello"")]
    internal static partial void Hello(this ILogger logger);
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LogMethodAttribute))! },
            new[] { OriginalSource },
            extraFile: "Log.cs").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task StringConstant()
    {
        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        const string Msg = ""Hello"";
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(Msg);
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
                        const string Msg = ""Hello"";
                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                        }
                    }
                }
                ";

        const string ExpectedTarget = @"
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(0, LogLevel.Trace, ""Hello"")]
    internal static partial void Hello(this ILogger logger);
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LogMethodAttribute))! },
            new[] { OriginalSource },
            extraFile: "Log.cs").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task ConcatenatedString()
    {
        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"" + "" "" + ""World"");
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
                            logger.HelloWorld();
                        }
                    }
                }
                ";

        const string ExpectedTarget = @"
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(0, LogLevel.Trace, ""Hello World"")]
    internal static partial void HelloWorld(this ILogger logger);
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LogMethodAttribute))! },
            new[] { OriginalSource },
            extraFile: "Log.cs").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task InterpolatedString()
    {
        const string OriginalSource = @"
                using Microsoft.Extensions.Logging;

                namespace Example
                {
                    public class TestClass
                    {
                        public void Test(this ILogger logger, int x)
                        {
                            logger.LogTrace($""Hello {x + 1}"");
                            logger.LogTrace($""Hello {x} World"");
                            logger.LogTrace($""Hello {x} {x + 1} World"");
                            logger.LogTrace(new System.Exception(), $""Hello {x} World"");
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
                        public void Test(this ILogger logger, int x)
                        {
                            logger.HelloArg0(x + 1);
                            logger.HelloXWorld(x);
                            logger.HelloXArg0World(x, x + 1);
                            logger.HelloXWorld(new System.Exception(), x);
                        }
                    }
                }
                ";

        const string ExpectedTarget = @"
#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(0, LogLevel.Trace, ""Hello {_arg0}"")]
    internal static partial void HelloArg0(this ILogger logger, int _arg0);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(1, LogLevel.Trace, ""Hello {x} World"")]
    internal static partial void HelloXWorld(this ILogger logger, int x);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(2, LogLevel.Trace, ""Hello {x} {_arg0} World"")]
    internal static partial void HelloXArg0World(this ILogger logger, int x, int _arg0);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(3, LogLevel.Trace, ""Hello {x} World"")]
    internal static partial void HelloXWorld(this ILogger logger, Exception exception, int x);
}
";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LogMethodAttribute))! },
            new[] { OriginalSource },
            extraFile: "Log.cs").ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task Basic()
    {
        const string OrriginalTarget = @"
                static partial class Log
                {
                }
                ";

        const string OrriginalSource = @"
                using Microsoft.Extensions.Logging;
                using System;

                namespace Example
                {
                    public class TestClass
                    {
                        private const string Message = ""Hello"";
                        private const LogLevel Level = LogLevel.Debug;
                        private const string NullMessage = null!;

                        public void Test(ILogger logger)
                        {
                            logger.LogTrace(""Hello"");
                            logger.LogTrace(""Hello {arg1}"", ""One"");
                            logger.LogTrace(new Exception(), ""Hello"");
                            logger.LogTrace(new Exception(), ""Hello {arg1}"", ""One"");

                            logger.LogDebug(""Hello"");
                            logger.LogDebug(""Hello {arg1}"", ""One"");
                            logger.LogDebug(new Exception(), ""Hello"");
                            logger.LogDebug(new Exception(), ""Hello {arg1}"", ""One"");

                            logger.LogInformation(""Hello"");
                            logger.LogInformation(""Hello {arg1}"", ""One"");
                            logger.LogInformation(new Exception(), ""Hello"");
                            logger.LogInformation(new Exception(), ""Hello {arg1}"", ""One"");

                            logger.LogWarning(""Hello"");
                            logger.LogWarning(""Hello {arg1}"", ""One"");
                            logger.LogWarning(new Exception(), ""Hello"");
                            logger.LogWarning(new Exception(), ""Hello {arg1}"", ""One"");

                            logger.LogError(""Hello"");
                            logger.LogError(""Hello {arg1}"", ""One"");
                            logger.LogError(new Exception(), ""Hello"");
                            logger.LogError(new Exception(), ""Hello {arg1}"", ""One"");

                            logger.LogCritical(""Hello"");
                            logger.LogCritical(""Hello {arg1}"", ""One"");
                            logger.LogCritical(new Exception(), ""Hello"");
                            logger.LogCritical(new Exception(), ""Hello {arg1}"", ""One"");

                            logger.Log(LogLevel.Trace, ""Hello"");
                            logger.Log(LogLevel.Debug, ""Hello"");
                            logger.Log(LogLevel.Information, ""Hello"");
                            logger.Log(LogLevel.Warning, ""Hello {arg1}"", ""One"");
                            logger.Log(LogLevel.Error, new Exception(), ""Hello"");
                            logger.Log(LogLevel.Critical, new Exception(), ""Hello {arg1}"", ""One"");

                            logger.Log(Level, Message);

                            logger.LogCritical(""Hello {arg1:0}"", ""One"");
                            logger.LogCritical(""Hello {arg1:0"", ""One"");
                            logger.LogCritical(""Hello {{arg1}}"");

                            logger.Log(LogLevel.Debug, new EventId(), ""Hello"");
                            logger.Log(LogLevel.Debug, new EventId(), new Exception(), ""Hello"");
                            logger.LogDebug(new EventId(), ""Hello"");
                            logger.LogDebug(new EventId(), new Exception(), ""Hello"");
                            logger.LogTrace("""");
                            logger.Log((LogLevel)42, ""Hello"");
                            logger.LogDebug(NullMessage);
                            logger.LogDebug(null!);
                            logger.Log((LogLevel)3.1415, ""Hello"");
                        }
                    }
                }
                ";

        const string ExpectedSource = @"
                using Microsoft.Extensions.Logging;
                using System;

                namespace Example
                {
                    public class TestClass
                    {
                        private const string Message = ""Hello"";
                        private const LogLevel Level = LogLevel.Debug;
                        private const string NullMessage = null!;

                        public void Test(ILogger logger)
                        {
                            logger.Hello();
                            logger.HelloArg1(""One"");
                            logger.Hello(new Exception());
                            logger.HelloArg1(new Exception(), ""One"");

                            logger.Hello2();
                            logger.HelloArg12(""One"");
                            logger.Hello2(new Exception());
                            logger.HelloArg12(new Exception(), ""One"");

                            logger.Hello2();
                            logger.HelloArg12(""One"");
                            logger.Hello2(new Exception());
                            logger.HelloArg12(new Exception(), ""One"");

                            logger.Hello2();
                            logger.HelloArg12(""One"");
                            logger.Hello2(new Exception());
                            logger.HelloArg12(new Exception(), ""One"");

                            logger.Hello2();
                            logger.HelloArg12(""One"");
                            logger.Hello2(new Exception());
                            logger.HelloArg12(new Exception(), ""One"");

                            logger.Hello2();
                            logger.HelloArg12(""One"");
                            logger.Hello2(new Exception());
                            logger.HelloArg12(new Exception(), ""One"");

                            logger.Hello();
                            logger.Hello();
                            logger.Hello();
                            logger.HelloArg1(""One"");
                            logger.Hello(new Exception());
                            logger.HelloArg1(new Exception(), ""One"");

                            logger.Hello();

                            logger.HelloArg10(""One"");
                            logger.HelloArg102(""One"");
                            logger.HelloArg1();

                            logger.Log(LogLevel.Debug, new EventId(), ""Hello"");
                            logger.Log(LogLevel.Debug, new EventId(), new Exception(), ""Hello"");
                            logger.LogDebug(new EventId(), ""Hello"");
                            logger.LogDebug(new EventId(), new Exception(), ""Hello"");
                            logger.LogTrace("""");
                            logger.Log((LogLevel)42, ""Hello"");
                            logger.LogDebug(NullMessage);
                            logger.LogDebug(null!);
                            logger.Log((LogLevel)3.1415, ""Hello"");
                        }
                    }
                }
                ";
        const string ExpectedTarget = @"
                static partial class Log
                {

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(0, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(1, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(2, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(3, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(4, Microsoft.Extensions.Logging.LogLevel.Debug, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(5, Microsoft.Extensions.Logging.LogLevel.Debug, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(6, Microsoft.Extensions.Logging.LogLevel.Debug, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(7, Microsoft.Extensions.Logging.LogLevel.Debug, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(8, Microsoft.Extensions.Logging.LogLevel.Information, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(9, Microsoft.Extensions.Logging.LogLevel.Information, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(10, Microsoft.Extensions.Logging.LogLevel.Information, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(11, Microsoft.Extensions.Logging.LogLevel.Information, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(12, Microsoft.Extensions.Logging.LogLevel.Warning, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(13, Microsoft.Extensions.Logging.LogLevel.Warning, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(14, Microsoft.Extensions.Logging.LogLevel.Warning, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(15, Microsoft.Extensions.Logging.LogLevel.Warning, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(16, Microsoft.Extensions.Logging.LogLevel.Error, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(17, Microsoft.Extensions.Logging.LogLevel.Error, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(18, Microsoft.Extensions.Logging.LogLevel.Error, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(19, Microsoft.Extensions.Logging.LogLevel.Error, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(20, Microsoft.Extensions.Logging.LogLevel.Critical, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(21, Microsoft.Extensions.Logging.LogLevel.Critical, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(22, Microsoft.Extensions.Logging.LogLevel.Critical, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(23, Microsoft.Extensions.Logging.LogLevel.Critical, ""Hello {arg1}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger, System.Exception exception, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(24, Microsoft.Extensions.Logging.LogLevel.Critical, ""Hello {arg1:0}"")]
    internal static partial void HelloArg10(this Microsoft.Extensions.Logging.ILogger logger, string arg1);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(25, Microsoft.Extensions.Logging.LogLevel.Critical, ""Hello {arg1:0"")]
    internal static partial void HelloArg10(this Microsoft.Extensions.Logging.ILogger logger, string arg0);

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(26, Microsoft.Extensions.Logging.LogLevel.Critical, ""Hello {{arg1}}"")]
    internal static partial void HelloArg1(this Microsoft.Extensions.Logging.ILogger logger);
}
                ";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LogMethodAttribute))! },
            new[] { OrriginalSource, OrriginalTarget }).ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }

    [Fact]
    public static async Task Nullable()
    {
        const string OrriginalTarget = @"
                    static partial class Log
                    {
                    }
                    ";

        const string OrriginalSource = @"
                    using Microsoft.Extensions.Logging;
                    using System;

                    namespace Example
                    {
                        public class TestClass
                        {
                            public void Test(ILogger? logger)
                            {
                                logger?.LogTrace(""Hello"");
                            }
                        }
                    }
                    ";

        const string ExpectedSource = @"
                    using Microsoft.Extensions.Logging;
                    using System;

                    namespace Example
                    {
                        public class TestClass
                        {
                            public void Test(ILogger? logger)
                            {
logger.Hello();
                            }
                        }
                    }
                    ";
        const string ExpectedTarget = @"
                    static partial class Log
                    {

    [Microsoft.Extensions.Telemetry.Logging.LogMethod(0, Microsoft.Extensions.Logging.LogLevel.Trace, ""Hello"")]
    internal static partial void Hello(this Microsoft.Extensions.Logging.ILogger? logger);
}
                    ";

        var l = await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new LegacyLoggingFixer(),
            new[] { Assembly.GetAssembly(typeof(ILogger))!, Assembly.GetAssembly(typeof(LogMethodAttribute))! },
            new[] { OrriginalSource, OrriginalTarget }).ConfigureAwait(false);

        var actualSource = l[0];
        var actualTarget = l[1];

        Assert.Equal(ExpectedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualSource);
        Assert.Equal(ExpectedTarget.Replace("\r\n", "\n", StringComparison.Ordinal), actualTarget);
    }
}
