// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.LocalAnalyzers.Resource.Test;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.CallAnalysis.Test;

public static class ToInvariantStringTests
{
    [Fact]
    public static async Task Basic()
    {
        const string Source = @"
            using System.Globalization;

            namespace Example
            {
                public class Test
                {
                    public void Foo()
                    {
                        byte v1 = 0;
                        short v2 = 0;
                        int v3 = 0;
                        long v4 = 0;

                        _ = /*0+*/v1.ToString(CultureInfo.InvariantCulture)/*-0*/;
                        _ = /*1+*/v2.ToString(CultureInfo.InvariantCulture)/*-1*/;
                        _ = /*2+*/v3.ToString(CultureInfo.InvariantCulture)/*-2*/;
                        _ = /*3+*/v4.ToString(CultureInfo.InvariantCulture)/*-3*/;
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
            Source.AssertDiagnostic(i, DiagDescriptors.ToInvariantString, d[i]);
        }
    }
}
