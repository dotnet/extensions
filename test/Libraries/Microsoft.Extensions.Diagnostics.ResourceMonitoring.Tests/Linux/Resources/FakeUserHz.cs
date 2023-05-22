// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

internal sealed class FakeUserHz : IUserHz
{
    public FakeUserHz(long value)
    {
        Value = value;
    }

    public long Value { get; }
}
