// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System.Net.Http;
using Azure.Core;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// Specifies configuration parameters such as the Azure AI project that should be used, and the credentials that
/// should be used, when a <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Content Safety service
/// to perform evaluations.
/// </summary>
/// <param name="credential">
/// The Azure <see cref="TokenCredential"/> that should be used when authenticating requests.
/// </param>
/// <param name="subscriptionId">
/// The ID of the Azure subscription that contains the project identified by <paramref name="projectName"/>.
/// </param>
/// <param name="resourceGroupName">
/// The name of the Azure resource group that contains the project identified by <paramref name="projectName"/>.
/// </param>
/// <param name="projectName">
/// The name of the Azure AI project.
/// </param>
/// <param name="httpClient">
/// The <see cref="System.Net.Http.HttpClient"/> that should be used when communicating with the Azure AI Content
/// Safety service. While the parameter is optional, it is recommended to supply an
/// <see cref="System.Net.Http.HttpClient"/> that is configured with robust resilience and retry policies.
/// </param>
/// <param name="timeoutInSecondsForRetries">
/// The timeout (in seconds) after which a <see cref="ContentSafetyEvaluator"/> should stop retrying failed attempts
/// to communicate with the Azure AI Content Safety service when performing evaluations.
/// </param>
public sealed class ContentSafetyServiceConfiguration(
    TokenCredential credential,
    string subscriptionId,
    string resourceGroupName,
    string projectName,
    HttpClient? httpClient = null,
    int timeoutInSecondsForRetries = 300) // 5 minutes
{
    /// <summary>
    /// Gets the Azure <see cref="TokenCredential"/> that should be used when authenticating requests.
    /// </summary>
    public TokenCredential Credential { get; } = credential;

    /// <summary>
    /// Gets the ID of the Azure subscription that contains the project identified by <see cref="ProjectName"/>.
    /// </summary>
    public string SubscriptionId { get; } = subscriptionId;

    /// <summary>
    /// Gets the name of the Azure resource group that contains the project identified by <see cref="ProjectName"/>.
    /// </summary>
    public string ResourceGroupName { get; } = resourceGroupName;

    /// <summary>
    /// Gets the name of the Azure AI project.
    /// </summary>
    public string ProjectName { get; } = projectName;

    /// <summary>
    /// Gets the <see cref="System.Net.Http.HttpClient"/> that should be used when communicating with the Azure AI
    /// Content Safety service.
    /// </summary>
    /// <remarks>
    /// While supplying an <see cref="System.Net.Http.HttpClient"/> is optional, it is recommended to supply one that
    /// is configured with robust resilience and retry policies.
    /// </remarks>
    public HttpClient? HttpClient { get; } = httpClient;

    /// <summary>
    /// Gets the timeout (in seconds) after which a <see cref="ContentSafetyEvaluator"/> should stop retrying failed
    /// attempts to communicate with the Azure AI Content Safety service when performing evaluations.
    /// </summary>
    public int TimeoutInSecondsForRetries { get; } = timeoutInSecondsForRetries;
}
