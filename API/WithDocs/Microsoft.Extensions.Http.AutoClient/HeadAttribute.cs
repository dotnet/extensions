// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines an API <c>HEAD</c> method.
/// </summary>
/// <remarks>
/// Marks a method within an interface annotated with <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientAttribute" /> as an API <c>HEAD</c> method.
///
/// The return type of an API method must be a <c>Task&lt;T&gt;</c>.
/// If T is a <see cref="T:System.String" /> and the dependency returns "text/plain" content type, the result will be the raw content of the response. Otherwise, it will be deserialized from JSON.
/// If T is of type <see cref="T:System.Net.Http.HttpResponseMessage" />, the result will be the actual response message without further processing.
///
/// If you provide an extra parameter to the method, you should use it between curly braces in the URL to make it an URL parameter. For example: <c>/api/users/{userId}</c>.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Head("/api/users/{userId}")]
///     Task&lt;UserHead&gt; HeadUserAsync(string userId, CancellationToken cancellationToken);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HeadAttribute : Attribute
{
    /// <summary>
    /// Gets the path of the request.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets or sets the name to use for this request within telemetry.
    /// </summary>
    /// <remarks>
    /// If this property is not provided, the request name is obtained from the method name.
    /// If the method name ends in 'Async', the request name will exclude that.
    /// For example, if the method is called <c>HeadUserAsync</c>, the request name, by default, will be <c>HeadUser</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// [AutoClient("MyClient")]
    /// interface IMyDependencyClient
    /// {
    ///     [Head("/api/users/{userId}", RequestName = "Head")]
    ///     Task&lt;UserHead&gt; HeadUserAsync(string userId, CancellationToken cancellationToken);
    /// }
    /// </code>
    /// </example>
    public string? RequestName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.HeadAttribute" /> class.
    /// </summary>
    /// <param name="path">The path of the request.</param>
    public HeadAttribute(string path);
}
