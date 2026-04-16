// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an <see cref="AIFunctionDeclaration"/> that signals to supporting AI services that deferred
/// loading should be used when tool search is enabled. Only the function's name and description are sent initially;
/// the full JSON schema is loaded on demand by the service when the model selects this tool.
/// </summary>
/// <remarks>
/// This class is a marker/decorator that signals to a supporting provider that the function should be
/// sent with deferred loading (only name and description upfront). Use <see cref="CreateToolSet"/> to create
/// a complete tool list including a <see cref="HostedToolSearchTool"/> and wrapped functions.
/// </remarks>
public sealed class SearchableAIFunctionDeclaration : DelegatingAIFunctionDeclaration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchableAIFunctionDeclaration"/> class.
    /// </summary>
    /// <param name="innerFunction">The <see cref="AIFunctionDeclaration"/> represented by this instance.</param>
    /// <param name="namespace">An optional namespace to group this function under for tool search.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="innerFunction"/> is <see langword="null"/>.</exception>
    public SearchableAIFunctionDeclaration(AIFunctionDeclaration innerFunction, string? @namespace = null)
        : base(innerFunction)
    {
        Namespace = @namespace;
    }

    /// <summary>Gets the namespace this function belongs to, or <see langword="null"/> if it is a standalone deferred function.</summary>
    public string? Namespace { get; }

    /// <summary>
    /// Creates a complete tool list with a <see cref="HostedToolSearchTool"/> and the given functions wrapped as <see cref="SearchableAIFunctionDeclaration"/>.
    /// </summary>
    /// <param name="functions">The functions to include as searchable tools.</param>
    /// <param name="namespace">An optional namespace to group the functions under.</param>
    /// <param name="toolSearchProperties">Any additional properties to pass to the <see cref="HostedToolSearchTool"/>.</param>
    /// <returns>A list of <see cref="AITool"/> instances ready for use in <see cref="ChatOptions.Tools"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="functions"/> is <see langword="null"/>.</exception>
    public static IList<AITool> CreateToolSet(
        IEnumerable<AIFunctionDeclaration> functions,
        string? @namespace = null,
        IReadOnlyDictionary<string, object?>? toolSearchProperties = null)
    {
        _ = Throw.IfNull(functions);

        var tools = new List<AITool> { new HostedToolSearchTool(toolSearchProperties) };
        foreach (var fn in functions)
        {
            tools.Add(new SearchableAIFunctionDeclaration(fn, @namespace));
        }

        return tools;
    }
}
