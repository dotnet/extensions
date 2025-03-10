// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a URL, typically to hosted content such as an image, audio, or video.
/// </summary>
/// <remarks>
/// This class is intended for use with HTTP or HTTPS URIs that reference hosted content.
/// For data URIs, use <see cref="DataContent"/> instead.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class UriContent : AIContent
{
    /// <summary>The URI represented.</summary>
    private Uri _uri;

    /// <summary>The MIME type of the data at the referenced URI.</summary>
    private string _mediaType;

    /// <summary>Initializes a new instance of the <see cref="UriContent"/> class.</summary>
    /// <param name="uri">The URI to the represented content.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="mediaType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    /// <exception cref="UriFormat"><paramref name="uri"/> is an invalid URL.</exception>
    /// <remarks>
    /// A media type must be specified, so that consumers know what to do with the content.
    /// If an exact media type is not known, but the category (e.g. image) is known, a wildcard
    /// may be used (e.g. "image/*").
    /// </remarks>
    public UriContent(string uri, string mediaType)
        : this(new Uri(Throw.IfNull(uri)), mediaType)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="UriContent"/> class.</summary>
    /// <param name="uri">The URI to the represented content.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="mediaType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    /// <remarks>
    /// A media type must be specified, so that consumers know what to do with the content.
    /// If an exact media type is not known, but the category (e.g. image) is known, a wildcard
    /// may be used (e.g. "image/*").
    /// </remarks>
    [JsonConstructor]
    public UriContent(Uri uri, string mediaType)
    {
        _uri = Throw.IfNull(uri);
        _mediaType = DataUriParser.ThrowIfInvalidMediaType(mediaType);
    }

    /// <summary>Gets or sets the <see cref="Uri"/> for this content.</summary>
    public Uri Uri
    {
        get => _uri;
        set => _uri = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the media type (also known as MIME type) for this content.</summary>
    public string MediaType
    {
        get => _mediaType;
        set => _mediaType = DataUriParser.ThrowIfInvalidMediaType(value);
    }

    /// <summary>
    /// Determines whether the <see cref="MediaType"/> has the specified prefix.
    /// </summary>
    /// <param name="prefix">The media type prefix.</param>
    /// <returns><see langword="true"/> if the <see cref="MediaType"/> has the specified prefix; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This performs an ordinal case-insensitive comparison of the <see cref="MediaType"/> against the specified <paramref name="prefix"/>.
    /// </remarks>
    public bool MediaTypeStartsWith(string prefix) =>
        MediaType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is true;

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Uri = {_uri}";
}
