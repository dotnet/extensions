// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class LegacyCollectionTests
{
    [Fact]
    public static async Task Basic()
    {
        const string Source = @"
            using System.Collections;
            using System.Collections.Specialized;

            namespace Example
            {
                public class Test
                {
                    public void Foo()
                    {
                        _ = /*0+*/new ArrayList()/*-0*/;
                        _ = /*1+*/new Hashtable()/*-1*/;
                        _ = /*2+*/new Queue()/*-2*/;
                        _ = /*3+*/new Stack()/*-3*/;
                        _ = /*4+*/new SortedList()/*-4*/;
                        _ = /*5+*/new HybridDictionary()/*-5*/;
                        _ = /*6+*/new ListDictionary()/*-6*/;
                        _ = /*7+*/new OrderedDictionary()/*-7*/;
                    }
                }
            }
        ";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            new[]
            {
                Assembly.GetAssembly(typeof(System.Collections.ArrayList))!,
                Assembly.GetAssembly(typeof(System.Collections.Queue))!,
                Assembly.GetAssembly(typeof(System.Collections.Specialized.HybridDictionary))!
            },
            new[] { Source }).ConfigureAwait(false);

        Assert.Equal(8, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.LegacyCollection, d[i]);
        }
    }
}
