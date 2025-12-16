// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET
using System.Buffers;
using System.Buffers.Text;
using System.ComponentModel;
#endif
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if !NET
using System.Runtime.InteropServices;
#endif
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable IDE0032 // Use auto property
#pragma warning disable CA1307 // Specify StringComparison for clarity

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
        // Store and validate the data URI.
        _uri = Throw.IfNullOrWhitespace(uri);
        if (!uri.StartsWith(DataUriParser.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            Throw.ArgumentException(nameof(uri), "The provided URI is not a data URI.");
        }

        // Parse the data URI to extract the data and media type.
        _dataUri = DataUriParser.Parse(uri.AsMemory());

        // Validate and store the media type.
        mediaType ??= _dataUri.MediaType;
        if (mediaType is null)
        {
            Throw.ArgumentNullException(nameof(mediaType), $"{nameof(uri)} did not contain a media type, and {nameof(mediaType)} was not provided.");
        }

        MediaType = DataUriParser.ThrowIfInvalidMediaType(mediaType);

        if (!_dataUri.IsBase64 || mediaType != _dataUri.MediaType)
        {
            // In rare cases, the data URI may contain non-base64 data, in which case we
            // want to normalize it to base64. The supplied media type may also be different
            // from the one in the data URI. In either case, we extract the bytes from the data URI
            // and then throw away the uri; we'll recreate it lazily in the canonical form.
            _data = _dataUri.ToByteArray();
            _dataUri = null;
            _uri = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="data">The byte contents.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mediaType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> represents an invalid media type.</exception>
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
    /// The returned URI is always a valid data URI string, even if the instance was constructed from a <see cref="ReadOnlyMemory{Byte}"/>
    /// or from a <see cref="System.Uri"/>.
    /// </remarks>
    [StringSyntax(StringSyntaxAttribute.Uri)]
#if NET
    [Description("A data URI representing the content.")]
#endif
    public string Uri
    {
        get
        {
            if (_uri is null)
            {
                Debug.Assert(_data is not null, "Expected _data to be initialized.");
                ReadOnlyMemory<byte> data = _data.GetValueOrDefault();

#if NET
                char[] array = ArrayPool<char>.Shared.Rent(
                    "data:".Length + MediaType.Length + ";base64,".Length + Base64.GetMaxEncodedToUtf8Length(data.Length));

                bool wrote = array.AsSpan().TryWrite($"data:{MediaType};base64,", out int prefixLength);
                wrote |= Convert.TryToBase64Chars(data.Span, array.AsSpan(prefixLength), out int dataLength);
                Debug.Assert(wrote, "Expected to successfully write the data URI.");
                _uri = array.AsSpan(0, prefixLength + dataLength).ToString();

                ArrayPool<char>.Shared.Return(array);
#else
                string base64 = MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment) ?
                    Convert.ToBase64String(segment.Array!, segment.Offset, segment.Count) :
                    Convert.ToBase64String(data.ToArray());

                _uri = $"data:{MediaType};base64,{base64}";
#endif
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

    /// <summary>Gets or sets an optional name associated with the data.</summary>
    /// <remarks>
    /// A service might use this name as part of citations or to help infer the type of data
    /// being represented based on a file extension.
    /// </remarks>
    public string? Name { get; set; }

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

    /// <summary>Gets the data represented by this instance as a Base64 character sequence.</summary>
    /// <returns>The base64 representation of the data.</returns>
    [JsonIgnore]
    public ReadOnlyMemory<char> Base64Data
    {
        get
        {
            string uri = Uri;
            int pos = uri.IndexOf(',');
            Debug.Assert(pos >= 0, "Expected comma to be present in the URI.");
            return uri.AsMemory(pos + 1);
        }
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            if (HasTopLevelMediaType("text"))
            {
                return $"MediaType = {MediaType}, Text = \"{Encoding.UTF8.GetString(Data.ToArray())}\"";
            }

            if ("application/json".Equals(MediaType, StringComparison.OrdinalIgnoreCase))
            {
                return $"JSON = {Encoding.UTF8.GetString(Data.ToArray())}";
            }

            const int MaxLength = 80;

            string uri = Uri;
            return uri.Length <= MaxLength ?
                $"Data = {uri}" :
                $"Data = {uri.Substring(0, MaxLength)}...";
        }
    }

    /// <summary>The default media type for unknown file extensions.</summary>
    private const string DefaultMediaType = "application/octet-stream";

    /// <summary>
    /// Loads a <see cref="DataContent"/> from a file path asynchronously.
    /// </summary>
    /// <param name="path">The file path to load the data from.</param>
    /// <param name="mediaType">
    /// The media type (also known as MIME type) represented by the content. If not provided,
    /// it will be inferred from the file extension. If it cannot be inferred, "application/octet-stream" is used.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="DataContent"/> containing the file data with the inferred or specified media type and name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    public static async Task<DataContent> LoadFromAsync(string path, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrEmpty(path);

        // Infer media type from the file extension if not provided
        mediaType ??= System.Net.Mime.MediaTypeMap.GetMediaType(path) ?? DefaultMediaType;

        // Read the file contents
#if NET
        byte[] data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
#else
        byte[] data;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
        {
            data = new byte[stream.Length];
            int totalRead = 0;
            while (totalRead < data.Length)
            {
                int read = await stream.ReadAsync(data, totalRead, data.Length - totalRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }
        }
#endif

        string? name = Path.GetFileName(path);
        return new DataContent(data, mediaType)
        {
            Name = string.IsNullOrEmpty(name) ? null : name
        };
    }

    /// <summary>
    /// Loads a <see cref="DataContent"/> from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to load the data from.</param>
    /// <param name="mediaType">
    /// The media type (also known as MIME type) represented by the content. If not provided and
    /// the stream is a <see cref="FileStream"/>, it will be inferred from the file extension.
    /// If it cannot be inferred, "application/octet-stream" is used.
    /// </param>
    /// <param name="name">
    /// The name to associate with the data. If not provided and the stream is a <see cref="FileStream"/>,
    /// the file name will be used.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="DataContent"/> containing the stream data with the inferred or specified media type and name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public static async Task<DataContent> LoadFromAsync(Stream stream, string? mediaType = null, string? name = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);

        // If the stream is a FileStream, try to infer media type and name from its path
        if (stream is FileStream fileStream)
        {
            string? filePath = fileStream.Name;
            if (name is null)
            {
                string? fileName = Path.GetFileName(filePath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    name = fileName;
                }
            }

            mediaType ??= System.Net.Mime.MediaTypeMap.GetMediaType(filePath);
        }

        // Fall back to default media type if still not set
        mediaType ??= DefaultMediaType;

        // Read the stream contents
        using var memoryStream = new MemoryStream();
#if NET
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
#else
        await stream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
#endif

        return new DataContent(new ReadOnlyMemory<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length), mediaType)
        {
            Name = name
        };
    }

    /// <summary>
    /// Saves the data content to a file asynchronously.
    /// </summary>
    /// <param name="path">
    /// The path to save the data to. If the path is an existing directory, the file name will be inferred
    /// from the <see cref="Name"/> property or a random name will be used.
    /// If the path does not have a file extension, an extension will be added based on the <see cref="MediaType"/>.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The actual path where the data was saved, which may include an inferred file name and/or extension.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public async Task<string> SaveToAsync(string path, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(path);

        string actualPath = path;

        // If path is a directory, infer the file name from the Name property or use a random name
        if (Directory.Exists(path))
        {
            string fileName = Name ?? Guid.NewGuid().ToString("N");
            actualPath = Path.Combine(path, fileName);
        }

        // Infer extension if path has no extension
        if (string.IsNullOrEmpty(Path.GetExtension(actualPath)))
        {
            string? extension = System.Net.Mime.MediaTypeMap.GetExtension(MediaType);
            if (!string.IsNullOrEmpty(extension))
            {
                actualPath += extension;
            }
        }

        // Write the data to the file
        ReadOnlyMemory<byte> data = Data;
#if NET9_0_OR_GREATER
        await File.WriteAllBytesAsync(actualPath, data, cancellationToken).ConfigureAwait(false);
#elif NET
        await File.WriteAllBytesAsync(actualPath, data.ToArray(), cancellationToken).ConfigureAwait(false);
#else
        using (var stream = new FileStream(actualPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            byte[] bytes = data.ToArray();
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        }
#endif

        return actualPath;
    }
}
