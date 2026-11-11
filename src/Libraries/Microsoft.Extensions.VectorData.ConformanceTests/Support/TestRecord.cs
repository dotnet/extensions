// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.VectorData;

namespace VectorData.ConformanceTests.Support;

public abstract class TestRecord<TKey>
{
    [VectorStoreKey]
    public TKey Key { get; set; } = default!;
}
