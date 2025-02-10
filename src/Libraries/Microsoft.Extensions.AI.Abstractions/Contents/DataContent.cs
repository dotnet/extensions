// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3996 // URI properties should not be strings
#pragma warning disable CA1056 // URI-like properties should not be strings

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents data content, such as an image or audio.
/// </summary>
/// <remarks>
/// <para>
/// The represented content may either be the actual bytes stored in this instance, or it may
/// be a URI that references the location of the content.
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

    /// <summary>The string-based representation of the URI, including any data in the instance.</summary>
    private string? _uri;

    /// <summary>The data, lazily initialized if the data is provided in a data URI.</summary>
    private ReadOnlyMemory<byte>? _data;

    /// <summary>Parsed data URI information.</summary>
    private DataUriParser.DataUri? _dataUri;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="uri">The URI of the content. This can be a data URI.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    public DataContent(Uri uri, string? mediaType = null)
        : this(Throw.IfNull(uri).ToString(), mediaType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="uri">The URI of the content. This can be a data URI.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    [JsonConstructor]
    public DataContent([StringSyntax(StringSyntaxAttribute.Uri)] string uri, string? mediaType = null)
    {
        _uri = Throw.IfNullOrWhitespace(uri);

        ValidateMediaType(ref mediaType);
        MediaType = mediaType;

        if (uri.StartsWith(DataUriParser.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            _dataUri = DataUriParser.Parse(uri.AsMemory());

            // If the data URI contains a media type that's different from a non-null media type
            // explicitly provided, prefer the one explicitly provided as an override.
            if (MediaType is not null)
            {
                if (MediaType != _dataUri.MediaType)
                {
                    // Extract the bytes from the data URI and null out the uri.
                    // Then we'll lazily recreate it later if needed based on the updated media type.
                    _data = _dataUri.ToByteArray();
                    _dataUri = null;
                    _uri = null;
                }
            }
            else
            {
                MediaType = _dataUri.MediaType;
            }
        }
        else if (!System.Uri.TryCreate(uri, UriKind.Absolute, out _))
        {
            throw new UriFormatException("The URI is not well-formed.");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="data">The byte contents.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    public DataContent(ReadOnlyMemory<byte> data, string? mediaType = null)
    {
        ValidateMediaType(ref mediaType);
        MediaType = mediaType;

        _data = data;
    }

    /// <summary>
    /// Determines whether the <see cref="MediaType"/> has the specified prefix.
    /// </summary>
    /// <param name="prefix">The media type prefix.</param>
    /// <returns><see langword="true"/> if the <see cref="MediaType"/> has the specified prefix, otherwise <see langword="false"/>.</returns>
    public bool MediaTypeStartsWith(string prefix)
        => MediaType?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is true;

    /// <summary>Sets <paramref name="mediaType"/> to null if it's empty or composed entirely of whitespace.</summary>
    private static void ValidateMediaType(ref string? mediaType)
    {
        if (!DataUriParser.IsValidMediaType(mediaType.AsSpan(), ref mediaType))
        {
            Throw.ArgumentException(nameof(mediaType), "Invalid media type.");
        }
    }

    /// <summary>Gets the URI for this <see cref="DataContent"/>.</summary>
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
                    Debug.Assert(Data is not null, "Expected Data to be initialized.");
                    _uri = string.Concat("data:", MediaType, ";base64,", Convert.ToBase64String(Data.GetValueOrDefault()
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
    /// Otherwise, this property returns null.
    /// </remarks>
    [JsonPropertyOrder(1)]
    public string? MediaType { get; private set; }

    /// <summary>Gets the data represented by this instance.</summary>
    /// <remarks>
    /// If the instance was constructed from a <see cref="ReadOnlyMemory{Byte}"/>, this property returns that data.
    /// If the instance was constructed from a data URI, this property the data contained within the data URI.
    /// If, however, the instance was constructed from another form of URI, one that simply references where the
    /// data can be found but doesn't actually contain the data, this property returns <see langword="null"/>;
    /// no attempt is made to retrieve the data from that URI.
    /// </remarks>
    [JsonIgnore]
    public ReadOnlyMemory<byte>? Data
    {
        get
        {
            if (_dataUri is not null)
            {
                _data ??= _dataUri.ToByteArray();
            }

            return _data;
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
            return uri.Length <= MaxLength ? uri : $"{uri.Substring(0, MaxLength)}...";
        }
    }
}
