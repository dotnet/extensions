// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to execute code it generates.</summary>
/// <remarks>
/// This tool does not itself implement code interpretation. It is a marker that can be used to inform a service
/// that the service is allowed to execute its generated code if the service is capable of doing so.
/// </remarks>
public class HostedCodeInterpreterTool : AITool
{
    /// <summary>Initializes a new instance of the <see cref="HostedCodeInterpreterTool"/> class.</summary>
    public HostedCodeInterpreterTool()
    {
    }
}
