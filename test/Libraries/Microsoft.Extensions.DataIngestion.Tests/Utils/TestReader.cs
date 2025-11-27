// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

internal sealed class TestReader : IIngestionDocumentReader<FileInfo>
{
    public TestReader(Func<FileInfo, string, string?, CancellationToken, Task<IngestionDocument>> readAsyncCallback)
    {
        ReadAsyncCallback = readAsyncCallback;
    }

    public Func<FileInfo, string, string?, CancellationToken, Task<IngestionDocument>> ReadAsyncCallback { get; }

    public Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
        => ReadAsyncCallback(source, identifier, mediaType, cancellationToken);
}
