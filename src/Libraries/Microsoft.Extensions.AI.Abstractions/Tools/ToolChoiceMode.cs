// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines modes that controls which if any tool is called by the model.
/// </summary>
[Experimental("MEAI001")]
public enum ToolChoiceMode
{
    /// <summary>
    /// The model will not call any tool and instead generates a message.
    /// </summary>
    None,

    /// <summary>
    /// The model can pick between generating a message or calling one or more tools.
    /// </summary>
    Auto,

    /// <summary>
    /// The model must call one or more tools.
    /// </summary>
    Required
}
