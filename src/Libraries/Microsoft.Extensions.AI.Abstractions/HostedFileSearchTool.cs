// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to execute code it generates.</summary>
/// <remarks>
/// This tool does not itself implement code interpretation. It is a marker that can be used to inform a service
/// that the service is allowed to execute its generated code if the service is capable of doing so.
/// </remarks>
public class HostedFileSearchTool : AITool
{
    /// <summary>Initializes a new instance of the <see cref="HostedFileSearchTool"/> class.</summary>
    public HostedFileSearchTool()
    {
    }

    /// <summary>Gets or sets a collection of <see cref="AIContent"/> to be used as input to the file search tool.</summary>
    /// <remarks>
    /// Services support different varied kinds of inputs. Most support the IDs of file stores that are hosted by the service,
    /// represented via <see cref="HostedVectorStoreContent"/>.
    /// </remarks>
    public IList<AIContent>? Inputs { get; set; }

    /// <summary>Gets or sets a requested limit on the number of matches the tool should produce.</summary>
    /// <remarks>This serves as a hint to the implementation and may not be respected.</remarks>
    public int? MaximumResultCount { get; set; }
}
