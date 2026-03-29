// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace VectorData.ConformanceTests.Support;

public abstract class DynamicVectorStoreCollectionFixture<TKey> : VectorStoreCollectionFixtureBase<object, Dictionary<string, object?>>
    where TKey : notnull
{
    protected abstract string KeyPropertyName { get; }

    public virtual async Task ReseedAsync()
    {
        // TODO: Use filtering delete, https://github.com/microsoft/semantic-kernel/issues/11830

        const int BatchSize = 100;

        int deletedCount;
        do
        {
            deletedCount = 0;
            await foreach (var record in Collection.GetAsync(r => true, top: BatchSize))
            {
                // TODO: We don't use batching delete because of https://github.com/microsoft/semantic-kernel/issues/13303
                await Collection.DeleteAsync((TKey)record[KeyPropertyName]!);
                deletedCount++;
            }
        }
        while (deletedCount == BatchSize);

        await SeedAsync();
    }
}
