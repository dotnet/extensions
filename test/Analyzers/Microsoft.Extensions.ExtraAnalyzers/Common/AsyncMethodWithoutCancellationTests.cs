// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public static class AsyncMethodWithoutCancellationTests
{
    [Fact]
    public static async Task WhenAsyncMethodWithoutCancellation_Failure()
    {
        const string Source = @"
            using System.Threading.Tasks;

            public class Test {
                /*0+*/public Task TestMethod() {
                    return Task.CompletedTask;
                }/*-0*/
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenAsyncMethodWithinObsoleteType_Ok()
    {
        const string Source = @"
            using System;
            using System.Threading.Tasks;

            [Obsolete]
            public class Test {
                public Task TestMethod() {
                    return Task.CompletedTask;
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenAsyncMethodIsObsolete_Ok()
    {
        const string Source = @"
            using System;
            using System.Threading.Tasks;

            public class Test {
                [Obsolete]
                public Task TestMethod() {
                    return Task.CompletedTask;
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenAsyncMethodWithCancellationToken_Ok()
    {
        const string Source = @"
            using System.Threading;
            using System.Threading.Tasks;

            public class Test {
                public Task TestMethod(CancellationToken _) {
                    return Task.CompletedTask;
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenAsyncMethodWithHttpContext_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Http;

            public class Test {
                public Task TestMethod(HttpContext context) {
                    return Task.CompletedTask;
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenAsyncResultWithoutCancellation_Failure()
    {
        const string Source = @"
            using System.Threading.Tasks;

            public class Test {
                /*0+*/public Task<bool> TestMethod(string _) {
                    return Task.FromResult(false);
                }/*-0*/
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenAsyncMethodIsInterface_Failure()
    {
        const string Source = @"
            using System.Threading.Tasks;

            public interface ITest {
                /*0+*/public Task TestMethod();/*-0*/
            }

            public class Test: ITest {
                public Task TestMethod() {
                    return Task.CompletedTask;
                }
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenAsyncMethodIsOverride_Failure()
    {
        const string Source = @"
            using System;
            using System.Threading.Tasks;

            public class Parent: IAsyncDisposable {
                public virtual ValueTask DisposeAsync() {
                    return default;
                }
            }

            public sealed class Child: Parent {
                public override ValueTask DisposeAsync() {
                    return default;
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    private static async Task RunAnalyzer(string source, int expected)
    {
        var d = await RoslynTestUtils.RunAnalyzer(
            new AsyncMethodWithoutCancellation(),
            new[] { Assembly.GetAssembly(typeof(HttpContext))! },
            new[] { source }).ConfigureAwait(false);

        for (int i = 0; i < d.Count; i++)
        {
            source.AssertDiagnostic(i, DiagDescriptors.AsyncMethodWithoutCancellation, d[i]);
        }

        d.Count.Should().Be(expected);
    }
}
