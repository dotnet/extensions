// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines the body for the API request.
/// </summary>
/// <remarks>
/// Marks a method parameter as the body for the request.
/// This attribute cannot be used with a <c>GET</c> or <c>HEAD</c> request.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Post("/api/users")]
///     Task&lt;User&gt; PostUserAsync([Body] User user, CancellationToken cancellationToken);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class BodyAttribute : Attribute
{
    /// <summary>
    /// Gets the body content type.
    /// </summary>
    public BodyContentType ContentType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.BodyAttribute" /> class.
    /// </summary>
    /// <remarks>
    /// This defaults to a body content type of <see cref="F:Microsoft.Extensions.Http.AutoClient.BodyContentType.ApplicationJson" />.
    /// </remarks>
    public BodyAttribute();

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.BodyAttribute" /> class.
    /// </summary>
    /// <param name="contentType">The content type to be used on the request content.</param>
    public BodyAttribute(BodyContentType contentType);
}
