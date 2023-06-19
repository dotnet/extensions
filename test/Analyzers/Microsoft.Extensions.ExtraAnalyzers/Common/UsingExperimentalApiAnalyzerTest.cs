// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public static class UsingExperimentalApiAnalyzerTest
{
    [Theory]
    [MemberData(nameof(TestData))]
    public static async Task Common(string source)
    {
        const string AttributeSource = @"
            namespace System.Diagnostics.CodeAnalysis
            {
                public sealed class ExperimentalAttribute : System.Attribute { }
            }";

        var d = await RoslynTestUtils.RunAnalyzer(
                new UsingExperimentalApiAnalyzer(),
                null,
                new[]
                {
                    source,
                    AttributeSource,
                }).ConfigureAwait(false);

        var expectedCount = source.CountSpans();
        Assert.Equal(expectedCount, d.Count);

        for (int i = 0; i < d.Count; i++)
        {
            source.AssertDiagnostic(i, DiagDescriptors.UsingExperimentalApi, d[i]);
        }
    }

    public static IEnumerable<object[]> TestData => new List<object[]>
    {
        new[]
        {
            @"
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            public class TestClass : /*0+*/ExpClass/*-0*/
            {
                /*1+*/ExpClass/*-1*/? _f0;

                public TestClass()
                {
                    _f0 = new /*2+*/ExpClass/*-2*/();
                    var l0 = new /*3+*/ExpClass/*-3*/();
                    /*4+*/ExpClass/*-4*/ l1 = new /*5+*/ExpClass/*-5*/();
                }

                public /*6+*/ExpClass/*-6*/? P0 { get; set; }
                public /*7+*/ExpClass/*-7*/? M0(/*8+*/ExpClass/*-8*/ ec) => null;

                public void Test()
                {
                    _ = typeof(/*9+*/ExpClass/*-9*/);
                    _ = nameof(/*10+*/ExpClass/*-10*/);
                    _ = new List</*11+*/ExpClass/*-11*/>();
                }
            }

            [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
            public class ExpClass
            {
            }
            "
        },

        new[]
        {
            @"
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            #pragma warning disable R9A029
            [assembly: Experimental]
            #pragma warning restore R9A029

            public class TestClass : /*0+*/ExpClass/*-0*/
            {
                /*1+*/ExpClass/*-1*/? _f0;

                public TestClass()
                {
                    _f0 = new /*2+*/ExpClass/*-2*/();
                    var l0 = new /*3+*/ExpClass/*-3*/();
                    /*4+*/ExpClass/*-4*/ l1 = new /*5+*/ExpClass/*-5*/();
                }

                public /*6+*/ExpClass/*-6*/? P0 { get; set; }
                public /*7+*/ExpClass/*-7*/? M0(/*8+*/ExpClass/*-8*/ ec) => null;

                public void Test()
                {
                    _ = typeof(/*9+*/ExpClass/*-9*/);
                    _ = nameof(/*10+*/ExpClass/*-10*/);
                    _ = new List</*11+*/ExpClass/*-11*/>();
                }
            }

            public class ExpClass
            {
                void Foo()
                {
                    _ = new /*12+*/TestClass/*-12*/();
                }
            }
            "
        },
    };
}
