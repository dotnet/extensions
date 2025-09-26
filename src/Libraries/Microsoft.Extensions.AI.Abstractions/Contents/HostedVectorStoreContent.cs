// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a vector store that is hosted by the AI service.
/// </summary>
/// <remarks>
/// Unlike <see cref="HostedFileContent"/> which represents a specific file that is hosted by the AI service,
/// <see cref="HostedVectorStoreContent"/> represents a vector store that can contain multiple files, indexed
/// for searching.
/// </remarks>
[DebuggerDisplay("VectorStoreId = {VectorStoreId}")]
public sealed class HostedVectorStoreContent : AIContent
{
    private string _vectorStoreId;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedVectorStoreContent"/> class.
    /// </summary>
    /// <param name="vectorStoreId">The ID of the hosted file store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vectorStoreId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="vectorStoreId"/> is empty or composed entirely of whitespace.</exception>
    public HostedVectorStoreContent(string vectorStoreId)
    {
        _vectorStoreId = Throw.IfNullOrWhitespace(vectorStoreId);
    }

    /// <summary>
    /// Gets or sets the ID of the hosted vector store.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    public string VectorStoreId
    {
        get => _vectorStoreId;
        set => _vectorStoreId = Throw.IfNullOrWhitespace(value);
    }
}
