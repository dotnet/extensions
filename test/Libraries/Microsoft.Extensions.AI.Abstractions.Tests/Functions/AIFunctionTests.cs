// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIFunctionTests
{
    [Fact]
    public async Task InvokeAsync_UsesDefaultEmptyCollectionForNullArgsAsync()
    {
        DerivedAIFunction f = new();

        using CancellationTokenSource cts = new();
        var result1 = ((IEnumerable<KeyValuePair<string, object?>>, CancellationToken))(await f.InvokeAsync(null, cts.Token))!;

        Assert.NotNull(result1.Item1);
        Assert.Empty(result1.Item1);
        Assert.Equal(cts.Token, result1.Item2);

        var result2 = ((IEnumerable<KeyValuePair<string, object?>>, CancellationToken))(await f.InvokeAsync(null, cts.Token))!;
        Assert.Same(result1.Item1, result2.Item1);
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        DerivedAIFunction f = new();
        Assert.Equal("name", f.ToString());
    }

    private sealed class DerivedAIFunction : AIFunction
    {
        public override string Name => "name";
        public override string Description => "";

        protected override Task<object?> InvokeCoreAsync(IEnumerable<KeyValuePair<string, object?>> arguments, CancellationToken cancellationToken)
        {
            Assert.NotNull(arguments);
            return Task.FromResult<object?>((arguments, cancellationToken));
        }
    }
}
