// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Triggers the generation of REST APIs and provides information about the HTTP client and, optionally, the name of the dependency.
/// </summary>
/// <remarks>
/// This attribute triggers the production of REST APIs and provides information about the HTTP client and optionally the name of the dependency.
/// It can only be applied to interfaces and their name must start with an 'I', for example <c>IMyClient</c>.
/// This attribute must receive as a first parameter the HTTP client name to be retrieved from the <see cref="IHttpClientFactory" />.
/// Optionally, it may receive a second attribute that will set the <c>dependency name</c> used in generated telemetry. If this value is not set, it will use the name of the interface
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
[Experimental(diagnosticId: Experiments.AutoClient, UrlFormat = Experiments.UrlFormat)]
[AttributeUsage(AttributeTargets.Interface)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class AutoClientAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoClientAttribute"/> class.
    /// </summary>
    /// <param name="httpClientName">The name of the HTTP client to be retrieved from <see cref="IHttpClientFactory" />.</param>
    public AutoClientAttribute(string httpClientName)
    {
        HttpClientName = httpClientName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoClientAttribute"/> class.
    /// </summary>
    /// <param name="httpClientName">The name of the HTTP client to be retrieved from <see cref="IHttpClientFactory" />.</param>
    /// <param name="customDependencyName">The dependency name override to be used with R9 Telemetry.</param>
    public AutoClientAttribute(string httpClientName, string customDependencyName)
    {
        HttpClientName = httpClientName;
        CustomDependencyName = customDependencyName;
    }

    /// <summary>
    /// Gets the HTTP client name of the API.
    /// </summary>
    public string HttpClientName { get; }

    /// <summary>
    /// Gets the custom dependency name of the API.
    /// </summary>
    public string? CustomDependencyName { get; }
}
