// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class EnumStringsTests
{
    [Fact]
    public static async Task EnumStrings()
    {
        const string Source = @"
            using System;

            namespace Example
            {
                public class TestClass
                {
                    public enum Color { Red, Green, Blue }

                    private void Foo()
                    {
                        var c = Color.Red;
                        _ = /*0+*/Enum.GetName(typeof(Color), c)/*-0*/;
                        _ = /*1+*/Enum.GetName<Color>(c)/*-1*/;
                        _ = /*2+*/c.ToString()/*-2*/;
                        _ = /*3+*/Color.Red.ToString()/*-3*/;
                    }
                }
            }";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            null,
            new[] { Source }).ConfigureAwait(false);

        Assert.Equal(4, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.EnumStrings, d[i]);
        }
    }
}
