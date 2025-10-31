// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform image generation.</summary>
/// <remarks>
/// This tool does not itself implement image generation. It is a marker that can be used to inform a service
/// that the service is allowed to perform image generation if the service is capable of doing so.
/// </remarks>
[Experimental("MEAI001")]
public class HostedImageGenerationTool : AITool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedImageGenerationTool"/> class with the specified options.
    /// </summary>
    public HostedImageGenerationTool()
        : base()
    {
    }

    /// <summary>
    /// Gets or sets the options used to configure image generation.
    /// </summary>
    public ImageGenerationOptions? Options { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the image generation tool from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IChatClient" /> implementation can have its own representation of this tool.
    /// When <see cref="IChatClient.GetResponseAsync" /> or <see cref="IChatClient.GetStreamingResponseAsync" /> is invoked with an
    /// <see cref="HostedImageGenerationTool" />, that implementation can convert the provided tool and options into its own representation in
    /// order to use it while performing the operation. For situations where a consumer knows which concrete <see cref="IChatClient" /> is being used
    /// and how it represents this tool, a new instance of that implementation-specific tool type can be returned by this
    /// callback for the <see cref="IChatClient" /> implementation to use instead of creating a new instance.
    /// Such implementations might mutate the supplied options instance further based on other settings supplied on this
    /// <see cref="HostedImageGenerationTool" /> instance or from other inputs, therefore, it is <b>strongly recommended</b> to not
    /// return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly typed
    /// properties on <see cref="ImageGenerationOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IChatClient?, object?>? RawRepresentationFactory { get; set; }
}
