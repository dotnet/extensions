// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging.Internals;

public class RandomizerTest
{
    [Fact]
    public void ParallelCalls_ShouldNotResultInException()
    {
        var randomizer = new Randomizer();
        var actions = Enumerable.Range(0, 1000).Select(_ =>
        {
#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
            return new Action(() =>
            {
                randomizer.NextInt(10000);
                randomizer.NextDouble(10000);
            });
        }).ToArray();
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

        Parallel.Invoke(actions);
    }

    [Fact]
    public void NextDouble_Ok()
    {
        var randomizer = new Randomizer();
        var ok = false;

        for (int i = 0; i < 10; i++)
        {
            if (randomizer.NextDouble(100000) > 1)
            {
                ok = true;
                break;
            }
        }

        Assert.True(ok);
    }
}
