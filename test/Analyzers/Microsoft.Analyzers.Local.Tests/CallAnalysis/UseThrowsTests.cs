// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.LocalAnalyzers.Resource.Test;
using Microsoft.Shared.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.CallAnalysis.Test;

public static class UseThrowsTests
{
    private const string ThrowHelpersClass = "Microsoft.Shared.Diagnostics.Throws.";
    private const string ThrowIfNullHelper = "Microsoft.Shared.Diagnostics.Throws.IfNull";

    private static readonly Assembly[] _references = new[]
    {
        Assembly.GetAssembly(typeof(Throw))!,
    };

    [Theory]
    [MemberData(nameof(SingleWarningData))]
    public static async Task ShouldFindSingleWarning(string original, string exception)
    {
        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            _references,
            new[] { original }).ConfigureAwait(false);

        Assert.Single(d);
        original.AssertDiagnostic(0, DiagDescriptors.ThrowsStatement, d[0]);
        Assert.Contains(ThrowHelpersClass + exception, d[0].GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(NoWarningData))]
    public static async Task ShouldNotProduceWarnings(string original)
    {
        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            _references,
            new[] { original }).ConfigureAwait(false);

        Assert.Empty(d);
    }

    public static IEnumerable<object[]> SingleWarningData =>
        new List<object[]>
        {
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value)
        {
            /*0+*/throw new ArgumentNullException(nameof(value));/*-0*/
        }
    }
}
",
"ArgumentNullException"
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value)
        {
            /*0+*/throw new ArgumentException(nameof(value));/*-0*/
        }
    }
}
",
"ArgumentException"
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value)
        {
            /*0+*/throw new ArgumentOutOfRangeException(nameof(value));/*-0*/
        }
    }
}
",
"ArgumentOutOfRangeException"
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value, int i)
        {
            if (i > value.Length)
            {
                /*0+*/throw new ArgumentOutOfRangeException(nameof(i));/*-0*/
            }
        }
    }
}
",
"ArgumentOutOfRangeException"
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static string Transform(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                /*0+*/throw new ArgumentException(nameof(key));/*-0*/
            }

            return key;
        }
    }
}
",
"ArgumentException"
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static string Transform(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                /*0+*/throw new ArgumentException($""Argument {nameof(key)} was null or empty."", nameof(key));/*-0*/
            }

            return key;
        }
    }
}
",
"ArgumentException"
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static string Transform(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                /*0+*/throw new ArgumentNullException(nameof(key), ""message"");/*-0*/
            }

            return key;
        }
    }
}
",
"ArgumentNullException"
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value, int i)
        {
            if (i > value.Length)
            {
                /*0+*/throw new ArgumentOutOfRangeException(nameof(i), ""message"");/*-0*/
            }
        }
    }
}
",
"ArgumentOutOfRangeException"
                },

        // temporarily disabled in order to roll out analyzer updates without changing
        // the rest of the source base. I'll start enabling the new analyzers and fixing
        // all the warnings in subsequent prs.
#if TURNED_OFF_FOR_ANALYZER_ROLLOUT
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value, int i)
        {
            /*0+*/throw new InvalidOperationException(""message"");/*-0*/
        }
    }
}
",
"InvalidOperationException"
                },
#endif
        };

    public static IEnumerable<object[]> NoWarningData =>
       new List<object[]>
       {
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value)
        {
            _ = value ?? throw new ArgumentException(nameof(value));
        }
    }
}
",
                },
                new object[]
                {
@"using System;

namespace Example
{
    public static class TestClass
    {
        public static void Test(string value)
        {
            var valNew = value == null ? throw new ArgumentNullException(nameof(value)) : value;
        }
    }
}
",
                },
       };

    [Theory]
    [MemberData(nameof(ExpressionSingleWarningData))]
    public static async Task Expression_ShouldFindSingleWarning(string original)
    {
        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            _references,
            new[] { original }).ConfigureAwait(false);

        Assert.Single(d);
        original.AssertDiagnostic(0, DiagDescriptors.ThrowsExpression, d[0]);
        Assert.Contains(ThrowIfNullHelper, d[0].GetMessage(), StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> ExpressionSingleWarningData =
        new List<object[]>
        {
                new object[]
                {
                    @"
                    using System;

                    namespace Example
                    {
                        public class TestClass
                        {
                            public static void Test(string value)
                            {
                                _ = /*0+*/value ?? throw new ArgumentNullException(nameof(value))/*-0*/;
                            }
                        }
                    }",
                },
                new object[]
                {
                    @"
                    using System;

                    namespace Example
                    {
                        public class TestClass
                        {
                            public static void Test(string value)
                            {
                                var newVal = /*0+*/value ?? throw new ArgumentNullException(nameof(value))/*-0*/;
                            }
                        }
                    }",
                }
        };
}
