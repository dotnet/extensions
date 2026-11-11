// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace VectorData.ConformanceTests.Support;

public abstract class VectorStoreCollectionFixture<TKey, TRecord> : VectorStoreCollectionFixtureBase<TKey, TRecord>
    where TKey : notnull
    where TRecord : TestRecord<TKey>
{
    public virtual async Task ReseedAsync()
    {
        // TODO: Use filtering delete, https://github.com/microsoft/semantic-kernel/issues/11830

        const int BatchSize = 100;

        TKey[] keys;
        do
        {
            keys = await Collection.GetAsync(r => true, top: BatchSize).Select(r => r.Key).ToArrayAsync();
            await Collection.DeleteAsync(keys);
        }
        while (keys.Length == BatchSize);

        await SeedAsync();
    }
}
