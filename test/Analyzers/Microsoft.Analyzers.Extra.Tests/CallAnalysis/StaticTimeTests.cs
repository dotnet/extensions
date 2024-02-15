// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class StaticTimeTests
{
    private static readonly Assembly[] _staticTimeReferences = new[]
    {
        Assembly.GetAssembly(typeof(Thread))!,
        Assembly.GetAssembly(typeof(Task))!,
        Assembly.GetAssembly(typeof(TimeSpan))!,
        Assembly.GetAssembly(typeof(DateTime))!,
    };

    [Fact]
    public static async Task StaticTime()
    {
        const string Source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace
            {
                public class TestClass
                {
                    public async Task TestMethod()
                    {
                        await /*0+*/Task.Delay(10)/*-0*/;
                        await /*1+*/Task.Delay(TimeSpan.FromSeconds(10))/*-1*/;
                        await /*2+*/Task.Delay(new TimeSpan())/*-2*/;

                        /*3+*/Thread.Sleep(10)/*-3*/;
                        /*4+*/Thread.Sleep(TimeSpan.FromSeconds(10))/*-4*/;
                        /*5+*/Thread.Sleep(new TimeSpan())/*-5*/;

                        _ = /*6+*/DateTime.UtcNow/*-6*/;
                        _ = /*7+*/DateTime.Now/*-7*/;
                        _ = /*8+*/DateTime.Today/*-8*/;

                        var now = /*9+*/DateTimeOffset.Now/*-9*/;
                        var utcNow = /*10+*/DateTimeOffset.UtcNow/*-10*/;
                    }

                    private readonly DateTime _currentTime = /*11+*/DateTime.UtcNow/*-11*/;
                    private DateTime CurrentTime { get; set; } = /*12+*/DateTime.UtcNow/*-12*/;

                    private DateTime GetTime(bool condition)
                    {
                        CurrentTime = /*13+*/DateTime.UtcNow/*-13*/;
                        var local = _currentTime;

                        return condition ? CurrentTime : local;
                    }

                    private readonly DateTimeOffset _currentTimeOffset = /*14+*/DateTimeOffset.UtcNow/*-14*/;
                    private DateTimeOffset CurrentTimeOffset { get; set; } = /*15+*/DateTimeOffset.UtcNow/*-15*/;

                    private DateTimeOffset GetTimeOffset(bool condition)
                    {
                        CurrentTimeOffset = /*16+*/DateTimeOffset.UtcNow/*-16*/;
                        var local = _currentTimeOffset;

                        return condition ? CurrentTimeOffset : local;
                    }
                }
            }";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            _staticTimeReferences,
            new[] { Source });

        Assert.Equal(17, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.StaticTime, d[i]);
        }
    }
}
