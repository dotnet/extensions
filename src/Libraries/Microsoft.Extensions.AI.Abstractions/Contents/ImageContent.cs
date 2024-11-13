// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents image content.
/// </summary>
public class ImageContent : DataContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageContent"/> class.
    /// </summary>
    /// <param name="uri">The URI of the content. This can be a data URI.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    public ImageContent(Uri uri, string? mediaType = null)
        : base(uri, mediaType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageContent"/> class.
    /// </summary>
    /// <param name="uri">The URI of the content. This can be a data URI.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    [JsonConstructor]
    public ImageContent([StringSyntax(StringSyntaxAttribute.Uri)] string uri, string? mediaType = null)
        : base(uri, mediaType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageContent"/> class.
    /// </summary>
    /// <param name="data">The byte contents.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    public ImageContent(ReadOnlyMemory<byte> data, string? mediaType = null)
        : base(data, mediaType)
    {
    }
}
