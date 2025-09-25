// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Options for configuring background responses.</summary>
[Experimental("MEAI001")]
public sealed class BackgroundResponsesOptions
{
    /// <summary>Initializes a new instance of the <see cref="BackgroundResponsesOptions"/> class.</summary>
    public BackgroundResponsesOptions()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="BackgroundResponsesOptions"/> class.</summary>
    /// <param name="options">The options to initialize from.</param>
    public BackgroundResponsesOptions(BackgroundResponsesOptions options)
    {
        _ = Throw.IfNull(options);

        Allow = options.Allow;
    }

    /// <summary>Gets or sets a value indicating whether the background responses are allowed.</summary>
    /// <remarks>
    /// This property only takes effect if the API it's used with supports background responses.
    /// If the API does not support background responses, this property will be ignored.
    /// </remarks>
    public bool? Allow { get; set; }
}
