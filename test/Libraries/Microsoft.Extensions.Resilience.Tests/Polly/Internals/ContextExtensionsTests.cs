// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Internal;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Internals;
public class ContextExtensionsTests
{
    [Fact]
    public void GetPolicyPipelineName_WhenNullPolicyKeyAndNullPolicyWrapKey_ShouldReturnEmpty()
    {
        var context = new Context();
        var name = context.GetPolicyPipelineName();
        Assert.Equal(string.Empty, name);
    }
}
