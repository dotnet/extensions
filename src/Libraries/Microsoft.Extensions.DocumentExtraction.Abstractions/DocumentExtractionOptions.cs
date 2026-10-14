// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Configures one extraction without depending on provider SDKs or AI abstractions.</summary>
/// <remarks>
/// Providers apply absent overrides using their documented defaults and reject unsupported explicit settings.
/// Provider-specific values are borrowed; callers must not mutate them during an operation.
/// Each provider must construct a fresh SDK request rather than mutate shared client defaults.
/// </remarks>
public sealed class DocumentExtractionOptions
{
    private string? _modelId;

    /// <summary>Gets or sets a per-call model or analyzer override, when supported.</summary>
    public string? ModelId
    {
        get => _modelId;
        set => _modelId = value is null ? null : Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Gets or sets a value indicating whether native output and available original response content are retained.</summary>
    public bool CaptureRawOutput { get; set; } = true;

    /// <summary>Gets adapter-specific overrides, including typed provider settings.</summary>
    public IDictionary<string, object?> AdditionalProperties { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Copies this request's scalars and property entries; opaque values remain borrowed.</summary>
    /// <returns>A separate request-options container.</returns>
    public DocumentExtractionOptions Clone()
    {
        DocumentExtractionOptions copy = new()
        {
            ModelId = ModelId,
            CaptureRawOutput = CaptureRawOutput
        };
        foreach (KeyValuePair<string, object?> property in AdditionalProperties)
        {
            copy.AdditionalProperties.Add(property.Key, property.Value);
        }

        return copy;
    }
}
