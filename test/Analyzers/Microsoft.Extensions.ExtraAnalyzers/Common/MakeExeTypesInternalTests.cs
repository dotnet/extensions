// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public static class MakeExeTypesInternalTests
{
    [Fact]
    public static async Task Basic()
    {
        const string Source = @"
namespace Example
{
    public static class /*0+*/Program/*-0*/
    {
        public static void Main()
        {
        }
    }

    public class /*1+*/Test/*-1*/
    {
    }

    internal class Test2
    {
        public class Test3
        {
        }

        internal class Test4
        {
        }
    }
}";

        const string ExpectedFixedSource = @"
namespace Example
{
    internal static class /*0+*/Program/*-0*/
    {
        public static void Main()
        {
        }
    }

    internal class /*1+*/Test/*-1*/
    {
    }

    internal class Test2
    {
        public class Test3
        {
        }

        internal class Test4
        {
        }
    }
}";

        var actualFixedSources = await RoslynTestUtils.RunAnalyzerAndFixer(
            new MakeExeTypesInternalAnalyzer(),
            new MakeExeTypesInternalFixer(),
            null,
            new[] { Source },
            asExecutable: true).ConfigureAwait(false);

        Assert.Equal(ExpectedFixedSource.Replace("\r\n", "\n", StringComparison.Ordinal), actualFixedSources[0]);
    }

    [Fact]
    public static async Task Disqualification()
    {
        const string Source = @"
#pragma warning disable R9A031
namespace Xunit
{
    public sealed class FactAttribute : System.Attribute {}
    public sealed class TheoryAttribute : System.Attribute {}
}

namespace Microsoft.AspNetCore.Mvc
{
    public sealed class HttpGetAttribute : System.Attribute {}
    public abstract class ControllerBase { }
}

namespace BenchmarkDotNet.Attributes
{
    public sealed class BenchmarkAttribute : System.Attribute {}
}

namespace MessagePack
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class MessagePackObjectAttribute : System.Attribute {}
}

#pragma warning restore R9A031
namespace Example
{
    using Xunit;
    using Microsoft.AspNetCore.Mvc;
    using BenchmarkDotNet.Attributes;
    using MessagePack;

    public static class /*0+*/Program/*-0*/
    {
        public static void Main()
        {
        }
    }

    public class Test1
    {
        [Fact]
        public void M() {}
    }

    public class Test2
    {
        [Theory]
        public void M() {}
    }

    public class Test3
    {
        [Benchmark]
        public void M() {}
    }

    public class Test4
    {
        [HttpGet]
        public void M() {}
    }

    public class Test5 : ControllerBase
    {
    }

    [MessagePackObject]
    public class Test6
    {
    }
}";

        var d = await RoslynTestUtils.RunAnalyzer(
            new MakeExeTypesInternalAnalyzer(),
            null,
            new[] { Source },
            asExecutable: true).ConfigureAwait(false);

        Assert.Equal(1, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.MakeExeTypesInternal, d[i]);
        }
    }

    [Fact]
    public static void UtilityMethods()
    {
        var f = new MakeExeTypesInternalFixer();
        Assert.Single(f.FixableDiagnosticIds);
        Assert.Equal(DiagDescriptors.MakeExeTypesInternal.Id, f.FixableDiagnosticIds[0]);
        Assert.Equal(WellKnownFixAllProviders.BatchFixer, f.GetFixAllProvider());
    }
}
