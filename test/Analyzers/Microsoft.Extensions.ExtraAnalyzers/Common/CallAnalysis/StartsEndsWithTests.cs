// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class StartsEndsWithTests
{
    [Fact]
    public static async Task FlagStartsWith()
    {
        const string Source = @"
                using System;
                using System.Globalization;

                namespace Example
                {
                    public static class TestClass
                    {
                        const string cc = ""J"";
                        public static void FlagMe()
                        {
                            string str = ""F"";
                            const string lc = ""I"";

                            _ = /*0+*/str.StartsWith(""F"")/*-0*/;
                            _ = /*1+*/str.StartsWith(""G"", StringComparison.Ordinal)/*-1*/;

                            _ = /*2+*/str.StartsWith(lc)/*-2*/;
                            _ = /*3+*/str.StartsWith(lc, StringComparison.Ordinal)/*-3*/;

                            _ = /*4+*/str.StartsWith(cc)/*-4*/;
                            _ = /*5+*/str.StartsWith(cc, StringComparison.Ordinal)/*-5*/;

                            _ = /*6+*/str.StartsWith($""K"")/*-6*/;
                            _ = /*7+*/str.StartsWith($""L"", StringComparison.Ordinal)/*-7*/;
                        }

                        public static void DontFlagMe()
                        {
                            string str = ""F"";

                            _ = str.StartsWith(""Fo"");
                            _ = str.StartsWith(""Fo"", StringComparison.OrdinalIgnoreCase);
                            _ = str.StartsWith(""Fo"", true, CultureInfo.InvariantCulture);

                            _ = str.StartsWith(""F"", StringComparison.OrdinalIgnoreCase);
                            _ = str.StartsWith(""F"", true, CultureInfo.InvariantCulture);
                        }
                    }
                }";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            null,
            new[] { Source }).ConfigureAwait(false);

#if ROSLYN_4_0_OR_GREATER
        Assert.Equal(8, d.Count);
#else
        Assert.Equal(6, d.Count);
#endif

        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.StartsEndsWith, d[i]);
        }
    }

    [Fact]
    public static async Task FlagEndsWith()
    {
        const string Source = @"
                using System;
                using System.Globalization;

                namespace Example
                {
                    public static class TestClass
                    {
                        const string cc = ""A"";

                        public static void FlagMe()
                        {
                            string str = ""B"";
                            const string lc = ""C"";

                            /*0+*/str.EndsWith(""D"")/*-0*/;
                            /*1+*/str.EndsWith(""E"", StringComparison.Ordinal)/*-1*/;

                            /*2+*/str.EndsWith(lc)/*-2*/;
                            /*3+*/str.EndsWith(lc, StringComparison.Ordinal)/*-3*/;

                            /*4+*/str.EndsWith(cc)/*-4*/;
                            /*5+*/str.EndsWith(cc, StringComparison.Ordinal)/*-5*/;

                            /*6+*/str.EndsWith($""F"")/*-6*/;
                            /*7+*/str.EndsWith($""G"", StringComparison.Ordinal)/*-7*/;
                        }

                        public static void DontFlagMe()
                        {
                            string str = ""H"";

                            _ = str.EndsWith(""Fo"");
                            _ = str.EndsWith(""Fo"", StringComparison.OrdinalIgnoreCase);
                            _ = str.EndsWith(""Fo"", true, CultureInfo.InvariantCulture);

                            _ = str.EndsWith(""I"", StringComparison.OrdinalIgnoreCase);
                            _ = str.EndsWith(""J"", true, CultureInfo.InvariantCulture);
                        }
                    }
                }";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            null,
            new[] { Source }).ConfigureAwait(false);

#if ROSLYN_4_0_OR_GREATER
        Assert.Equal(8, d.Count);
#else
        Assert.Equal(6, d.Count);
#endif

        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.StartsEndsWith, d[i]);
        }
    }
}
