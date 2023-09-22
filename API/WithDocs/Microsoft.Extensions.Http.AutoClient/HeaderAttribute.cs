// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines a header to be used in the API request.
/// </summary>
/// <remarks>
/// Marks a method parameter as a header to insert in the request.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Get("/api/users")]
///     Task&lt;string&gt; GetUsersAsync([Header("X-UserName")] string userName, CancellationToken cancellationToken);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class HeaderAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.HeaderAttribute" /> class.
    /// </summary>
    /// <param name="header">The name of the header.</param>
    public HeaderAttribute(string header);
}
