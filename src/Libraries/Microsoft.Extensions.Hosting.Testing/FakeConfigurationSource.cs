// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.Hosting;

internal sealed class FakeConfigurationSource : MemoryConfigurationSource
{
    public FakeConfigurationSource(params KeyValuePair<string, string?>[] initialData)
    {
        InitialData = initialData;
    }
}
