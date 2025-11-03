// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a file that is hosted by the AI service.
/// </summary>
/// <remarks>
/// Unlike <see cref="DataContent"/> which contains the data for a file or blob, this class represents a file that is hosted
/// by the AI service and referenced by an identifier. Such identifiers are specific to the provider.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class HostedFileContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedFileContent"/> class.
    /// </summary>
    /// <param name="fileId">The ID of the hosted file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fileId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileId"/> is empty or composed entirely of whitespace.</exception>
    public HostedFileContent(string fileId)
    {
        FileId = fileId;
    }

    /// <summary>
    /// Gets or sets the ID of the hosted file.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    public string FileId
    {
        get => field;
        set => field = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Gets or sets an optional media type (also known as MIME type) associated with the file.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> represents an invalid media type.</exception>
    public string? MediaType
    {
        get;
        set => field = value is not null ? DataUriParser.ThrowIfInvalidMediaType(value) : value;
    }

    /// <summary>Gets or sets an optional name associated with the file.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Determines whether the <see cref="MediaType"/>'s top-level type matches the specified <paramref name="topLevelType"/>.
    /// </summary>
    /// <param name="topLevelType">The type to compare against <see cref="MediaType"/>.</param>
    /// <returns><see langword="true"/> if the type portion of <see cref="MediaType"/> matches the specified value; otherwise, false.</returns>
    /// <remarks>
    /// <para>
    /// A media type is primarily composed of two parts, a "type" and a "subtype", separated by a slash ("/").
    /// The type portion is also referred to as the "top-level type"; for example,
    /// "image/png" has a top-level type of "image". <see cref="HasTopLevelMediaType"/> compares
    /// the specified <paramref name="topLevelType"/> against the type portion of <see cref="MediaType"/>.
    /// </para>
    /// <para>
    /// If <see cref="MediaType"/> is <see langword="null"/>, this method returns <see langword="false"/>.
    /// </para>
    /// </remarks>
    public bool HasTopLevelMediaType(string topLevelType) => MediaType is not null && DataUriParser.HasTopLevelMediaType(MediaType, topLevelType);

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = $"FileId = {FileId}";

            if (MediaType is string mediaType)
            {
                display += $", MediaType = {mediaType}";
            }

            if (Name is string name)
            {
                display += $", Name = \"{name}\"";
            }

            return display;
        }
    }
}
