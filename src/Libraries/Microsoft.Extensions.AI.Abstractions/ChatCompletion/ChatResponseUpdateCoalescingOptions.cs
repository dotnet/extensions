// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides options for configuring how <see cref="ChatResponseUpdate"/> instances are coalesced
/// when converting them to <see cref="ChatMessage"/> instances.
/// </summary>
[Experimental("EXTAI0001")]
public class ChatResponseUpdateCoalescingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to replace existing <see cref="DataContent"/> items
    /// when a new <see cref="DataContent"/> item with the same <see cref="DataContent.Name"/> is encountered.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to replace existing <see cref="DataContent"/> items with the same name;
    /// <see langword="false"/> to keep all <see cref="DataContent"/> items. The default is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// When this property is <see langword="true"/>, if a <see cref="DataContent"/> item is being added
    /// and there's already a <see cref="DataContent"/> item in the content list with the same
    /// <see cref="DataContent.Name"/>, the existing item will be replaced with the new one.
    /// This is useful for scenarios where updated data should override previous data with the same identifier.
    /// </remarks>
    public bool ReplaceDataContentWithSameName { get; set; }
}
