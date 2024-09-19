// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class SplitTests
{
    [Fact]
    public static async Task FlagStringSplit()
    {
        const string Source = @"
                using System;

                namespace Example
                {
                    public static class TestClass
                    {
                        public static void TestMethod()
                        {
                            var splits = /*0+*/""MyString"".Split('a', 0, StringSplitOptions.None)/*-0*/;
                        }
                    }
                }";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            null,
            new[] { Source });

        Assert.Single(d);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.Split, d[i]);
        }
    }
}
