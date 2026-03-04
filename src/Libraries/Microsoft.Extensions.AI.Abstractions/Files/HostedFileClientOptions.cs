// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for a hosted file client request.</summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedFileClientOptions
{
    /// <summary>
    /// Gets or sets a provider-specific scope or location identifier for the file operation.
    /// </summary>
    /// <remarks>
    /// Some providers use scoped storage for files. For example, OpenAI uses containers
    /// to scope code interpreter files. If specified, the operation will target
    /// files within the specified scope.
    /// </remarks>
    public string? Scope { get; set; }

    /// <summary>Gets or sets the purpose of a file.</summary>
    /// <remarks>
    /// <para>
    /// For creation operations, this typically indicates the intended use of the file being created, which may influence how the provider processes or validates the file.
    /// For listing operations, this typically filters the returned files to those matching the specified purpose.
    /// </para>
    /// <para>
    /// If not specified, implementations may default to a provider-specific value
    /// (typically "assistants" or equivalent for code interpreter use).
    /// Common values include "assistants", "fine-tune", "batch", and "vision",
    /// but the specific values supported depend on the provider.
    /// </para>
    /// </remarks>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the maximum number of files to return in a list operation.</summary>
    /// <remarks>
    /// If not specified, the provider's default limit will be used.
    /// </remarks>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the file operation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IHostedFileClient" /> implementation may have its own representation of options.
    /// When an operation is invoked with a <see cref="HostedFileClientOptions" />, that implementation may convert
    /// the provided options into its own representation in order to use it while performing the operation.
    /// For situations where a consumer knows which concrete <see cref="IHostedFileClient" /> is being used
    /// and how it represents options, a new instance of that implementation-specific options type may be returned
    /// by this callback, for the <see cref="IHostedFileClient" /> implementation to use instead of creating a new
    /// instance. Such implementations may mutate the supplied options instance further based on other settings
    /// supplied on this <see cref="HostedFileClientOptions" /> instance or from other inputs,
    /// therefore, it is <b>strongly recommended</b> to not return shared instances and instead make the callback
    /// return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly typed
    /// properties on <see cref="HostedFileClientOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IHostedFileClient, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Gets or sets additional properties for the request.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
