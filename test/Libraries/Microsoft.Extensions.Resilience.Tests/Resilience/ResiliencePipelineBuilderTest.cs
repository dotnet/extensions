// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test;

public sealed class ResiliencePipelineBuilderTest
{
    [Fact]
    public void Constructor_NullArgument_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new ResiliencePipelineBuilder<string>(null!, "test"));
        Assert.Throws<ArgumentNullException>(() => new ResiliencePipelineBuilder<string>(null!, string.Empty));
        Assert.Throws<ArgumentNullException>(() => new ResiliencePipelineBuilder<string>(new ServiceCollection(), null!));
        Assert.Throws<ArgumentException>(() => new ResiliencePipelineBuilder<string>(new ServiceCollection(), string.Empty));
    }
}
