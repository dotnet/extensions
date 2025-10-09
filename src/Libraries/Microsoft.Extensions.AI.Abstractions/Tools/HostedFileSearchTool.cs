// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform file search operations.</summary>
/// <remarks>
/// This tool is designed to facilitate file search functionality within AI services. It allows the service to search
/// for relevant content based on the provided inputs and constraints, such as the maximum number of results.
/// </remarks>
public class HostedFileSearchTool : AITool
{
    /// <summary>Initializes a new instance of the <see cref="HostedFileSearchTool"/> class.</summary>
    public HostedFileSearchTool()
    {
    }

    /// <inheritdoc />
    public override string Name => "file_search";

    /// <summary>Gets or sets a collection of <see cref="AIContent"/> to be used as input to the file search tool.</summary>
    /// <remarks>
    /// If no explicit inputs are provided, the service determines what inputs should be searched. Different services
    /// support different kinds of inputs, for example, some might respect <see cref="HostedFileContent"/> using provider-specific file IDs,
    /// others might support binary data uploaded as part of the request in <see cref="DataContent"/>, and others might support
    /// content in a hosted vector store and represented by a <see cref="HostedVectorStoreContent"/>.
    /// </remarks>
    public IList<AIContent>? Inputs { get; set; }

    /// <summary>Gets or sets a requested bound on the number of matches the tool should produce.</summary>
    public int? MaximumResultCount { get; set; }
}
