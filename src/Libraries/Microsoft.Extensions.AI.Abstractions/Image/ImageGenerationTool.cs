// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform image generation.</summary>
/// <remarks>
/// This tool does not itself implement image generation. It is a marker that can be used to inform a service
/// that the service is allowed to perform image generation if the service is capable of doing so.
/// </remarks>
[Experimental("MEAI001")]
public class ImageGenerationTool : AITool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageGenerationTool"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options to configure the image generation request. If <paramref name="options"/> is <see langword="null"/>, default options will be used.</param>
    public ImageGenerationTool(ImageGenerationOptions? options = null)
        : base()
    {
        AdditionalProperties = new AdditionalPropertiesDictionary(new Dictionary<string, object?>
        {
            [nameof(ImageGenerationOptions)] = options
        });
    }

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }
}
