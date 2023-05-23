// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class StringFormatTests
{
    [Fact]
    public static async Task ShouldFindWarning()
    {
        const int ExpectedNumberOfWarnings = 7; // update number of expected warnings

        const string Source = @"
            #pragma warning disable CS8767

            using System;
            using System.Text;

            namespace Example
            {
                public class TestClass
                {
                    private static string GetFormat()
                    {
                        return ""format"";
                    }
                        
                    public static void Test()
                    {
                        /*0+*/string.Format(""test"")/*-0*/;
                        /*1+*/String.Format(""test"")/*-1*/;
                        /*2+*/System.String.Format(""test"")/*-2*/;
                        var format = GetFormat();
                        string.Format(format);
                        /*3+*/string.Format(""test"", 1, 2)/*-3*/;
                        StringBuilder sb = new StringBuilder();
                        /*4+*/sb.AppendFormat(""test"", 1, 2)/*-4*/;
                        var textString = /*5+*/string.Format(""test"", 1, 2)/*-5*/;
                        sb.AppendFormat(textString, 1, 2);
                        string.Format(null, ""test"");
                            
                        F f = new F();
                        /*6+*/string.Format(f, ""test"")/*-6*/;
                            
                    }
                }

                interface I
                {
                    public string i();
                }
                    
                public class F : IFormatProvider, I
                {
                    public object GetFormat(Type formatType)
                    {
                        return this;
                    }
                        
                    public string i()
                    {
                        return ""test"";
                    }
                }
            }";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            null,
            new[] { Source }).ConfigureAwait(false);

        Assert.Equal(ExpectedNumberOfWarnings, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.StringFormat, d[i]);
        }
    }
}
