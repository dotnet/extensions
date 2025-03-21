// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIFunctionTests
{
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

        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            Assert.NotNull(arguments);
            return new ValueTask<object?>((arguments, cancellationToken));
        }
    }
}
