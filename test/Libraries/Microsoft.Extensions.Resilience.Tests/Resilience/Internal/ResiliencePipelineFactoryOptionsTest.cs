// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;
public class ResiliencePipelineFactoryOptionsTest
{
    [Fact]
    public void BuilderActions_EnsureNotNull()
    {
        var options = new ResiliencePipelineFactoryOptions<string>();

        Assert.NotNull(options.BuilderActions);
    }
}
