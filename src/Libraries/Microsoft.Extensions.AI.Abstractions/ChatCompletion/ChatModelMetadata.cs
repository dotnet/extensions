// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about a model used with an <see cref="IChatClient"/>.</summary>
public class ChatModelMetadata
{
    /// <summary>
    /// Gets a value indicating whether the model can produce structured output conforming to a JSON schema.
    /// </summary>
    public bool? SupportsNativeJsonSchema { get; init; }
}
