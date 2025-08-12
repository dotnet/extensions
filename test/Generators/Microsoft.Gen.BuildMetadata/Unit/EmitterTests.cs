// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.Gen.BuildMetadata.Test;

[Collection("BuildMetadataEmitterTests")]
public class EmitterTests
{
    [Fact]
    public void GivenCancellation_ShouldThrowException()
    {
        Assert.Throws<OperationCanceledException>(() =>
            _ = new Emitter().Emit(new CancellationToken(true)));
    }
}
