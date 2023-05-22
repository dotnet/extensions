// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;
public sealed class NoopChangeTokenTests
{
    private readonly NoopChangeToken _token = new();

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        Assert.False(_token.HasChanged);
        Assert.True(_token.ActiveChangeCallbacks);

        var dummyObject = "apples";
        var disposable = _token.RegisterChangeCallback(dummyObject => { }, dummyObject);
        Assert.NotNull(disposable);
    }
}
