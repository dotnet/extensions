// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents metadata about a file hosted by an AI service.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class HostedFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedFile"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or composed entirely of whitespace.</exception>
    public HostedFile(string id)
    {
        Id = Throw.IfNullOrWhitespace(id);
    }

    /// <summary>Gets the unique identifier of the file.</summary>
    public string Id { get; }

    /// <summary>Gets or sets the name of the file.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the media type (MIME type) of the file.</summary>
    public string? MediaType { get; set; }

    /// <summary>Gets or sets the size of the file in bytes.</summary>
    public long? SizeInBytes { get; set; }

    /// <summary>Gets or sets when the file was created.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the purpose for which the file was uploaded.</summary>
    /// <remarks>
    /// Common values include "assistants", "fine-tune", "batch", or "vision",
    /// but the specific values supported depend on the provider.
    /// </remarks>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets additional properties associated with the file.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets or sets the raw representation of the file from the underlying provider.</summary>
    /// <remarks>
    /// If the <see cref="HostedFile"/> was created from an underlying provider's response,
    /// this property contains the original response object.
    /// </remarks>
    public object? RawRepresentation { get; set; }

    /// <summary>
    /// Creates a <see cref="HostedFileContent"/> that references this file.
    /// </summary>
    /// <returns>A new <see cref="HostedFileContent"/> instance referencing this file.</returns>
    public HostedFileContent ToHostedFileContent() =>
        new(Id)
        {
            Name = Name,
            MediaType = MediaType
        };

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = $"Id = {Id}";

            if (Name is not null)
            {
                display += $", Name = \"{Name}\"";
            }

            if (MediaType is not null)
            {
                display += $", MediaType = {MediaType}";
            }

            if (SizeInBytes is not null)
            {
                display += $", Size = {SizeInBytes} bytes";
            }

            return display;
        }
    }
}
