// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public static class AsyncCallInsideUsingBlockAnalyzerTests
{
    private const string BoilerPlate = @"
using System;
using System.Threading.Tasks;

public class Disposable: IDisposable
{
    public void Dispose() => throw new NotSupportedException();
}

public partial class Test {
    public static Task DoAsync(IDisposable _)
    {
        return Task.CompletedTask;
    }

    public static ValueTask DoVAsync(IDisposable _)
    {
        return default(ValueTask);
    }

    public static Task<bool> DoAsyncResult(IDisposable _)
    {
        return Task.FromResult(false);
    }

    public static void DoSync(IDisposable _)
    {
    }
}
";

    [Fact]
    public static async Task WhenTaskIsAwaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;

            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        await DoAsync(disposable);
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenTaskInUsingDeclarationIsAwaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;

            public partial class Test {
                public static async Task TestMethod() {
                    using var disposable = new Disposable();
                    await DoAsync(disposable);
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenTaskIsAwaitedWithConfigureAwait_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;

            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        await DoAsync(disposable).ConfigureAwait(false);
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenTaskIsAssignedAndAwaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;

            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        var t = DoAsync(disposable);
                        await t;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenDisposableInLambda_Ignored()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        _ = Task.Run(() => DoAsync(disposable));
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenTaskIsNotAwaited_Fires()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        /*0+*/DoAsync(disposable)/*-0*/;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenDeclaredVarIsNotAwaited_Fires()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        var t = /*0+*/DoAsync(disposable)/*-0*/;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenTaskIsSyncWaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        DoAsync(disposable).Wait();
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenAssignedVarIsSyncWaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        Task t;
                        t = DoAsync(disposable);
                        t.Wait();
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenAssignedVarIsNotAwaited_Fires()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        Task t;
                        t = /*0+*/DoAsync(disposable)/*-0*/;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenAssignedVarIsAwaitedWithWhenAll_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        var t = DoAsync(disposable);
                        await Task.WhenAll(t);
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenValueTaskIsNotAwaited_Fires()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        /*0+*/DoVAsync(disposable)/*-0*/;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenTaskResultIsNotAwaited_Fires()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        /*0+*/DoAsyncResult(disposable)/*-0*/;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 1);
    }

    [Fact]
    public static async Task WhenDeclaredResultVarIsSyncWaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        var t = DoAsyncResult(disposable);
                        _ = t.Result;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenResultIsSyncWaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        _ = DoAsyncResult(disposable).Result;
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenResultIsGetAwaiterWaited_Ok()
    {
        const string Source = @"
            using System.Threading.Tasks;
            #pragma warning disable CS4014
            #pragma warning disable CS1998
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        _ = DoAsyncResult(disposable).GetAwaiter().GetResult();
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenDisposableInArrow_Ignored()
    {
        const string Source = @"
            using System.Threading;
            using System.Threading.Tasks;
            #pragma warning disable CS1998
            #pragma warning disable CS8321
            public partial class Test {
                public static async Task TestMethod() {
                    using (var disposable = new Disposable())
                    {
                        Task<Disposable?> Execute(CancellationToken token) => Task.FromResult(disposable);
                    }
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    [Fact]
    public static async Task WhenInvocationIsSync_Ok()
    {
        const string Source = @"
            public partial class Test {
                public static void TestMethod() {
                    using var disposable = new Disposable();
                    DoSync(disposable);
                }
            }
        ";

        await RunAnalyzer(Source, 0);
    }

    private static async Task RunAnalyzer(string source, int expected)
    {
        var d = await RoslynTestUtils.RunAnalyzer(
            new AsyncCallInsideUsingBlockAnalyzer(),
            null,
            new[] { BoilerPlate, source }).ConfigureAwait(false);

        for (int i = 0; i < d.Count; i++)
        {
            source.AssertDiagnostic(i, DiagDescriptors.AsyncCallInsideUsingBlock, d[i]);
        }

        d.Count.Should().Be(expected);
    }
}
