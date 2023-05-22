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

public class AsyncPolicyPipelineTests
{
    public static IEnumerable<object[]> GetRange() => Enumerable.Range(1, 10).Select(i => new object[] { i });

    private class TestNoOpPolicy : AsyncPolicy
    {
        public bool Visited { get; private set; }

        protected override async Task<T> ImplementationAsync<T>(
            Func<Context, CancellationToken, Task<T>> action,
            Context context,
            CancellationToken cancellationToken,
            bool continueOnCapturedContext)
        {
            ((List<TestNoOpPolicy>)context["order"]).Add(this);

            Visited = true;
            return await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext);
        }
    }

    [Fact]
    public void Ctor_WhenEmptyArgument_ShouldThrow()
    {
        var policies = Array.Empty<IAsyncPolicy>();

        Assert.Throws<ArgumentException>(() => new AsyncPolicyPipeline(policies));
    }

    [Theory]
    [MemberData(nameof(GetRange))]
    public async Task ExecuteAsync_ShouldVisitAllPolicies(int numberOfPolicies)
    {
        var policies = Enumerable.Range(0, numberOfPolicies)
            .Select(_ => new TestNoOpPolicy())
            .Cast<IAsyncPolicy>()
            .ToArray();
        var wrapperPolicy = new AsyncPolicyPipeline(policies);
        var expectedString = "expected";
        var expectedInt = 10;
        var expectedBool = true;
        var context = new Context();
        var order = new List<TestNoOpPolicy>();
        context["order"] = order;

        var resultString = await wrapperPolicy.ExecuteAsync(_ =>
        {
            ((List<TestNoOpPolicy>)context["order"]).Add(null!);
            return Task.FromResult(expectedString);
        },
        context);

        var resultInt = await wrapperPolicy.ExecuteAsync(_ =>
        {
            ((List<TestNoOpPolicy>)context["order"]).Add(null!);
            return Task.FromResult(expectedInt);
        },
        context);

        var resultBool = await wrapperPolicy.ExecuteAsync(_ =>
        {
            ((List<TestNoOpPolicy>)context["order"]).Add(null!);
            return Task.FromResult(expectedBool);
        },
        context);

        Assert.Equal(expectedString, resultString);
        Assert.Equal(expectedInt, resultInt);
        Assert.Equal(expectedBool, resultBool);

        Assert.All(policies, policy => Assert.True(((TestNoOpPolicy)policy).Visited));
        Assert.Null(order.Last());

        for (int i = 0; i < policies.Length; i++)
        {
            var item = order[i];
            Assert.Equal(policies[i], order[i]);
        }
    }
}
