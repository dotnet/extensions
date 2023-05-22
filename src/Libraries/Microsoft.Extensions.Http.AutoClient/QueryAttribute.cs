// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines a query string to be used in the API request.
/// </summary>
/// <remarks>
/// Marks a method parameter as a query string for the request.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Get("/api/users")]
///     Task&lt;string&gt; GetUsersAsync([Query] string userName, [Query("id")] string userId, CancellationToken cancellationToken = default);
/// }
/// </code>
/// </example>
[Experimental]
[AttributeUsage(AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class QueryAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    /// <remarks>
    /// This overloaded uses the name of the associated method parameter as the query string key.
    /// </remarks>
    public QueryAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    /// <param name="key">The query key to use in the request.</param>
    public QueryAttribute(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Gets the query key, if set.
    /// </summary>
    public string? Key { get; }
}
