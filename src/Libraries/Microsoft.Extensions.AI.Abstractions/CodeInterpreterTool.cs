// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Shared.Collections;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a tool that can be specified to an AI service to enable it to execute code it generates.</summary>
/// <remarks>
/// This tool does not itself implement code interpration. It is a marker that can be used to inform a service
/// that the service is allowed to execute its generated code if the service is capable of doing so.
/// </remarks>
public class CodeInterpreterTool : AITool
{
    /// <summary>Initializes a new instance of the <see cref="CodeInterpreterTool"/> class.</summary>
    public CodeInterpreterTool(IReadOnlyDictionary<string, object?>? additionalProperties = null)
    {
        AdditionalProperties = additionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
    }

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }
}
