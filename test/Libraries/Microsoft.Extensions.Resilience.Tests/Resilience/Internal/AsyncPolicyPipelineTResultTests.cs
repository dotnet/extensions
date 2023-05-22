// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Internal;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public class AsyncPolicyPipelineTResultTests
{
    public static IEnumerable<object[]> GetRange() => Enumerable.Range(1, 10).Select(i => new object[] { i });

    private class TestNoOpPolicy<T> : AsyncPolicy<T>
    {
        public bool Visited { get; private set; }

        protected override async Task<T> ImplementationAsync(
            Func<Context, CancellationToken, Task<T>> action,
            Context context,
            CancellationToken cancellationToken,
            bool continueOnCapturedContext)
        {
            ((List<TestNoOpPolicy<T>>)context["order"]).Add(this);

            Visited = true;
            return await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext);
        }
    }

    [Fact]
    public void Ctor_WhenEmptyArgument_ShouldThrow()
    {
        var policies = Array.Empty<IAsyncPolicy<string>>();

        Assert.Throws<ArgumentException>(() => new AsyncPolicyPipeline<string>(policies));
    }

    [Theory]
    [MemberData(nameof(GetRange))]
    public async Task ExecuteAsync_ShouldVisitAllPolicies(int numberOfPolicies)
    {
        var policies = Enumerable.Range(0, numberOfPolicies)
            .Select(_ => new TestNoOpPolicy<string>())
            .Cast<IAsyncPolicy<string>>()
            .ToArray();
        var wrapperPolicy = new AsyncPolicyPipeline<string>(policies);
        var expected = "expected";
        var context = new Context();
        var order = new List<TestNoOpPolicy<string>>();
        context["order"] = order;

        var result = await wrapperPolicy.ExecuteAsync(_ =>
        {
            ((List<TestNoOpPolicy<string>>)context["order"]).Add(null!);
            return Task.FromResult(expected);
        },
        context);

        Assert.Equal(expected, result);
        Assert.All(policies, policy => Assert.True(((TestNoOpPolicy<string>)policy).Visited));
        Assert.Null(order.Last());

        for (int i = 0; i < policies.Length; i++)
        {
            var item = order[i];
            Assert.Equal(policies[i], order[i]);
        }
    }
}
