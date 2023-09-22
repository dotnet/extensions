// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Triggers the generation of REST APIs and provides information about the HTTP client and, optionally, the name of the dependency.
/// </summary>
/// <remarks>
/// This attribute triggers the production of REST APIs and provides information about the HTTP client and optionally the name of the dependency.
/// It can only be applied to interfaces and their name must start with an 'I', for example <c>IMyClient</c>.
/// This attribute must receive as a first parameter the HTTP client name to be retrieved from the <see cref="T:System.Net.Http.IHttpClientFactory" />.
/// Optionally, it may receive a second parameter that will set the <c>dependency name</c> used in generated telemetry. If this value is not set, it will use the name of the interface
/// without the leading 'I'.
/// If the interface name ends in 'Client' or 'Api', the dependency name will exclude that. Example: <c>IMyDependencyClient</c> would result in dependency name <c>MyDependency</c>.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Interface)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class AutoClientAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP client name of the API.
    /// </summary>
    public string HttpClientName { get; }

    /// <summary>
    /// Gets the custom dependency name of the API. This is used in generated telemetry.
    /// </summary>
    /// <remarks>
    /// If this value is not set, then for the dependency name it will use the name of the interface without the leading 'I' with trimming 'Client' or 'Api' at the end.
    /// </remarks>
    public string? CustomDependencyName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientAttribute" /> class.
    /// </summary>
    /// <param name="httpClientName">The name of the HTTP client to be retrieved from <see cref="T:System.Net.Http.IHttpClientFactory" />.</param>
    public AutoClientAttribute(string httpClientName);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientAttribute" /> class.
    /// </summary>
    /// <param name="httpClientName">The name of the HTTP client to be retrieved from <see cref="T:System.Net.Http.IHttpClientFactory" />.</param>
    /// <param name="customDependencyName">The dependency name override to use.</param>
    public AutoClientAttribute(string httpClientName, string customDependencyName);
}
