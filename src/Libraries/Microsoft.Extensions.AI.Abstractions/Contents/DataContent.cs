// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3996 // URI properties should not be strings
#pragma warning disable CA1054 // URI-like parameters should not be strings
#pragma warning disable CA1056 // URI-like properties should not be strings

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents binary content with an associated media type (also known as MIME type).
/// </summary>
/// <remarks>
/// <para>
/// The content represents in-memory data. For references to data at a remote URI, use <see cref="UriContent"/> instead.
/// </para>
/// <para>
/// <see cref="Uri"/> always returns a valid URI string, even if the instance was constructed from
/// a <see cref="ReadOnlyMemory{T}"/>. In that case, a data URI will be constructed and returned.
/// </para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class DataContent : AIContent
{
    // Design note:
    // Ideally DataContent would be based in terms of Uri. However, Uri has a length limitation that makes it prohibitive
    // for the kinds of data URIs necessary to support here. As such, this type is based in strings.

    /// <summary>Parsed data URI information.</summary>
    private readonly DataUriParser.DataUri? _dataUri;

    /// <summary>The string-based representation of the URI, including any data in the instance.</summary>
    private string? _uri;

    /// <summary>The data, lazily initialized if the data is provided in a data URI.</summary>
    private ReadOnlyMemory<byte>? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="uri">The data URI containing the content.</param>
    /// <param name="mediaType">
    /// The media type (also known as MIME type) represented by the content. If not provided,
    /// it must be provided as part of the <paramref name="uri"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> is not a data URI.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> did not contain a media type and <paramref name="mediaType"/> was not supplied.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    public DataContent(Uri uri, string? mediaType = null)
        : this(Throw.IfNull(uri).ToString(), mediaType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="uri">The data URI containing the content.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> is not a data URI.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> did not contain a media type and <paramref name="mediaType"/> was not supplied.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    [JsonConstructor]
    public DataContent([StringSyntax(StringSyntaxAttribute.Uri)] string uri, string? mediaType = null)
    {
        _uri = Throw.IfNullOrWhitespace(uri);

        if (!uri.StartsWith(DataUriParser.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            Throw.ArgumentException(nameof(uri), "The provided URI is not a data URI.");
        }

        _dataUri = DataUriParser.Parse(uri.AsMemory());

        if (mediaType is null)
        {
            mediaType = _dataUri.MediaType;
            if (mediaType is null)
            {
                Throw.ArgumentNullException(nameof(mediaType), $"{nameof(uri)} did not contain a media type, and {nameof(mediaType)} was not provided.");
            }
        }
        else
        {
            if (mediaType != _dataUri.MediaType)
            {
                // If the data URI contains a media type that's different from a non-null media type
                // explicitly provided, prefer the one explicitly provided as an override.

                // Extract the bytes from the data URI and null out the uri.
                // Then we'll lazily recreate it later if needed based on the updated media type.
                _data = _dataUri.ToByteArray();
                _dataUri = null;
                _uri = null;
            }
        }

        MediaType = DataUriParser.ThrowIfInvalidMediaType(mediaType);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="data">The byte contents.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mediaType"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is empty or composed entirely of whitespace.</exception>
    public DataContent(ReadOnlyMemory<byte> data, string mediaType)
    {
        MediaType = DataUriParser.ThrowIfInvalidMediaType(mediaType);

        _data = data;
    }

    /// <summary>
    /// Determines whether the <see cref="MediaType"/>'s top-level type matches the specified <paramref name="topLevelType"/>.
    /// </summary>
    /// <param name="topLevelType">The type to compare against <see cref="MediaType"/>.</param>
    /// <returns><see langword="true"/> if the type portion of <see cref="MediaType"/> matches the specified value; otherwise, false.</returns>
    /// <remarks>
    /// A media type is primarily composed of two parts, a "type" and a "subtype", separated by a slash ("/").
    /// The type portion is also referred to as the "top-level type"; for example,
    /// "image/png" has a top-level type of "image". <see cref="HasTopLevelMediaType"/> compares
    /// the specified <paramref name="topLevelType"/> against the type portion of <see cref="MediaType"/>.
    /// </remarks>
    public bool HasTopLevelMediaType(string topLevelType) => DataUriParser.HasTopLevelMediaType(MediaType, topLevelType);

    /// <summary>Gets the data URI for this <see cref="DataContent"/>.</summary>
    /// <remarks>
    /// The returned URI is always a valid URI string, even if the instance was constructed from a <see cref="ReadOnlyMemory{Byte}"/>
    /// or from a <see cref="System.Uri"/>. In the case of a <see cref="ReadOnlyMemory{T}"/>, this property returns a data URI containing
    /// that data.
    /// </remarks>
    [StringSyntax(StringSyntaxAttribute.Uri)]
    public string Uri
    {
        get
        {
            if (_uri is null)
            {
                if (_dataUri is null)
                {
                    Debug.Assert(_data is not null, "Expected _data to be initialized.");
                    _uri = string.Concat("data:", MediaType, ";base64,", Convert.ToBase64String(_data.GetValueOrDefault()
#if NET
                        .Span));
#else
                        .Span.ToArray()));
#endif
                }
                else
                {
                    _uri = _dataUri.IsBase64 ?
#if NET
                        $"data:{MediaType};base64,{_dataUri.Data.Span}" :
                        $"data:{MediaType};,{_dataUri.Data.Span}";
#else
                        $"data:{MediaType};base64,{_dataUri.Data}" :
                        $"data:{MediaType};,{_dataUri.Data}";
#endif
                }
            }

            return _uri;
        }
    }

    /// <summary>Gets the media type (also known as MIME type) of the content.</summary>
    /// <remarks>
    /// If the media type was explicitly specified, this property returns that value.
    /// If the media type was not explicitly specified, but a data URI was supplied and that data URI contained a non-default
    /// media type, that media type is returned.
    /// </remarks>
    [JsonIgnore]
    public string MediaType { get; }

    /// <summary>Gets the data represented by this instance.</summary>
    /// <remarks>
    /// If the instance was constructed from a <see cref="ReadOnlyMemory{Byte}"/>, this property returns that data.
    /// If the instance was constructed from a data URI, this property the data contained within the data URI.
    /// If, however, the instance was constructed from another form of URI, one that simply references where the
    /// data can be found but doesn't actually contain the data, this property returns <see langword="null"/>;
    /// no attempt is made to retrieve the data from that URI.
    /// </remarks>
    [JsonIgnore]
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            if (_data is null)
            {
                Debug.Assert(_dataUri is not null, "Expected dataUri to be initialized.");
                _data = _dataUri!.ToByteArray();
            }

            Debug.Assert(_data is not null, "Expected data to be initialized.");
            return _data.GetValueOrDefault();
        }
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            const int MaxLength = 80;

            string uri = Uri;
            return uri.Length <= MaxLength ?
                $"Data = {uri}" :
                $"Data = {uri.Substring(0, MaxLength)}...";
        }
    }
}
