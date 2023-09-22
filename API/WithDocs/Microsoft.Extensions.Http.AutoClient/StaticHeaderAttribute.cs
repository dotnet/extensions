// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines a static header to be sent on every API request.
/// </summary>
/// <remarks>
/// Injects a static header to be sent with every request. When this attribute is applied
/// to an interface, then it impacts every method described by the interface. Otherwise, it only
/// affects the method where it is applied.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// [StaticHeader("X-MyHeader", "MyHeaderValue")]
/// interface IMyDependencyClient
/// {
///     [Get("/api/users")]
///     [StaticHeader("X-GetUsersHeader", "Value")]
///     public Task&lt;Users&gt; GetUsers(CancellationToken cancellationToken = default);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class StaticHeaderAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Gets the value of the header.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.StaticHeaderAttribute" /> class.
    /// </summary>
    /// <param name="header">The name of the header.</param>
    /// <param name="value">The value of the header.</param>
    public StaticHeaderAttribute(string header, string value);
}
