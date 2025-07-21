// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a file store (such as a vector store) that is hosted by the AI service.
/// </summary>
/// <remarks>
/// Unlike <see cref="HostedFileContent"/> which represents a specific file that is hosted by the AI service,
/// <see cref="HostedFileStoreContent"/> represents a file store that can contain multiple files.
/// </remarks>
[DebuggerDisplay("FileStoreId = {FileStoreId}")]
public sealed class HostedFileStoreContent : AIContent
{
    private string _fileId;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedFileStoreContent"/> class.
    /// </summary>
    /// <param name="fileStoreId">The ID of the hosted file store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fileStoreId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileStoreId"/> is empty or composed entirely of whitespace.</exception>
    public HostedFileStoreContent(string fileStoreId)
    {
        _fileId = Throw.IfNullOrWhitespace(fileStoreId);
    }

    /// <summary>
    /// Gets or sets the ID of the hosted file store.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    public string FileStoreId
    {
        get => _fileId;
        set => _fileId = Throw.IfNullOrWhitespace(value);
    }
}
