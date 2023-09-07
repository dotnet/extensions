// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
[AttributeUsage(AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class QueryAttribute : Attribute
{
    /// <summary>
    /// Gets the query key, if set.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.QueryAttribute" /> class.
    /// </summary>
    /// <remarks>
    /// This overload uses the name of the associated method parameter as the query string key.
    /// </remarks>
    public QueryAttribute();

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.QueryAttribute" /> class.
    /// </summary>
    /// <param name="key">The query key to use in the request.</param>
    public QueryAttribute(string key);
}
