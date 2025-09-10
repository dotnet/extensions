// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Azure.Core;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// Specifies configuration parameters, such as the Azure AI Foundry project and the credentials
/// that should be used, when a <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Foundry Evaluation
/// service to perform evaluations.
/// </summary>
/// <remarks>
/// <para>
/// Azure AI Foundry supports two kinds of projects - Hub-based projects and non-Hub-based projects (also
/// known simply as Foundry projects). See <see href="https://learn.microsoft.com/azure/ai-foundry/how-to/create-projects">Create a project for Azure AI Foundry</see>.
/// </para>
/// <para>
/// Hub-based projects are configured by specifying the <see cref="SubscriptionId"/>,
/// <see cref="ResourceGroupName"/>, and <see cref="ProjectName"/> for the project. Non-Hub-based projects, on the
/// other hand, are configured by specifying only the <see cref="Endpoint"/> for the project. Use the appropriate
/// constructor overload to initialize <see cref="ContentSafetyServiceConfiguration"/> based on the kind of project you
/// are working with.
/// </para>
/// </remarks>
public sealed class ContentSafetyServiceConfiguration
{
    private const int DefaultTimeoutInSecondsForRetries = 300; // 5 minutes

    /// <summary>
    /// Gets the Azure <see cref="TokenCredential"/> that should be used when authenticating requests.
    /// </summary>
    public TokenCredential Credential { get; }

    /// <summary>
    /// Gets the ID of the Azure subscription that contains the project identified by <see cref="ProjectName"/> if the
    /// project is a Hub-based project.
    /// </summary>
    public string? SubscriptionId { get; }

    /// <summary>
    /// Gets the name of the Azure resource group that contains the project identified by <see cref="ProjectName"/> if
    /// the project is a Hub-based project.
    /// </summary>
    public string? ResourceGroupName { get; }

    /// <summary>
    /// Gets the name of the Azure AI Foundry project if the project is a Hub-based project.
    /// </summary>
    public string? ProjectName { get; }

    /// <summary>
    /// Gets the endpoint for the Azure AI Foundry project if the project is a non-Hub-based project.
    /// </summary>
    public Uri? Endpoint { get; }

    /// <summary>
    /// Gets the <see cref="HttpClient"/> that should be used when communicating with the Azure AI Foundry Evaluation
    /// service.
    /// </summary>
    /// <remarks>
    /// While supplying an <see cref="HttpClient"/> is optional, it is recommended to supply one that is configured
    /// with robust resilience and retry policies.
    /// </remarks>
    public HttpClient? HttpClient { get; }

    /// <summary>
    /// Gets the timeout (in seconds) after which a <see cref="ContentSafetyEvaluator"/> should stop retrying failed
    /// attempts to communicate with the Azure AI Foundry Evaluation service when performing evaluations.
    /// </summary>
    public int TimeoutInSecondsForRetries { get; }

    [MemberNotNullWhen(true, nameof(SubscriptionId), nameof(ResourceGroupName), nameof(ProjectName))]
    [MemberNotNullWhen(false, nameof(Endpoint))]
    internal bool IsHubBasedProject =>
        !string.IsNullOrWhiteSpace(SubscriptionId) &&
        !string.IsNullOrWhiteSpace(ResourceGroupName) &&
        !string.IsNullOrWhiteSpace(ProjectName) &&
        Endpoint is null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSafetyServiceConfiguration"/> class for a Hub-based Azure
    /// AI Foundry project with the specified <paramref name="projectName"/>.
    /// </summary>
    /// <param name="credential">
    /// The Azure <see cref="TokenCredential"/> that should be used when authenticating requests.
    /// </param>
    /// <param name="subscriptionId">
    /// The ID of the Azure subscription that contains the Hub-based AI Foundry project identified by
    /// <paramref name="projectName"/>.
    /// </param>
    /// <param name="resourceGroupName">
    /// The name of the Azure resource group that contains the Hub-based AI Foundry project identified by
    /// <paramref name="projectName"/>.
    /// </param>
    /// <param name="projectName">
    /// The name of the Hub-based Azure AI Foundry project.
    /// </param>
    /// <param name="httpClient">
    /// The <see cref="HttpClient"/> that should be used when communicating with the Azure AI Foundry Evaluation
    /// service. While the parameter is optional, it is recommended to supply an <see cref="HttpClient"/> that is
    /// configured with robust resilience and retry policies.
    /// </param>
    /// <param name="timeoutInSecondsForRetries">
    /// The timeout (in seconds) after which a <see cref="ContentSafetyEvaluator"/> should stop retrying failed
    /// attempts to communicate with the Azure AI Foundry Evaluation service when performing evaluations.
    /// </param>
    /// <remarks>
    /// <para>
    /// Azure AI Foundry supports two kinds of projects - Hub-based projects and non-Hub-based projects (also
    /// known simply as Foundry projects). See <see href="https://learn.microsoft.com/azure/ai-foundry/how-to/create-projects">Create a project for Azure AI Foundry</see>.
    /// </para>
    /// <para>
    /// Use this constructor overload if you are working with a Hub-based project.
    /// </para>
    /// </remarks>
    public ContentSafetyServiceConfiguration(
        TokenCredential credential,
        string subscriptionId,
        string resourceGroupName,
        string projectName,
        HttpClient? httpClient = null,
        int timeoutInSecondsForRetries = DefaultTimeoutInSecondsForRetries)
    {
        Credential = Throw.IfNull(credential);
        SubscriptionId = Throw.IfNullOrWhitespace(subscriptionId);
        ResourceGroupName = Throw.IfNullOrWhitespace(resourceGroupName);
        ProjectName = Throw.IfNullOrWhitespace(projectName);
        HttpClient = httpClient;
        TimeoutInSecondsForRetries = timeoutInSecondsForRetries;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSafetyServiceConfiguration"/> class for a non-Hub-based
    /// Azure AI Foundry project with the specified <paramref name="endpoint"/>.
    /// </summary>
    /// <param name="credential">
    /// The Azure <see cref="TokenCredential"/> that should be used when authenticating requests.
    /// </param>
    /// <param name="endpoint">
    /// The endpoint for the non-Hub-based Azure AI Foundry project.
    /// </param>
    /// <param name="httpClient">
    /// The <see cref="HttpClient"/> that should be used when communicating with the Azure AI Foundry Evaluation
    /// service. While the parameter is optional, it is recommended to supply an <see cref="HttpClient"/> that is
    /// configured with robust resilience and retry policies.
    /// </param>
    /// <param name="timeoutInSecondsForRetries">
    /// The timeout (in seconds) after which a <see cref="ContentSafetyEvaluator"/> should stop retrying failed
    /// attempts to communicate with the Azure AI Foundry Evaluation service when performing evaluations.
    /// </param>
    /// <remarks>
    /// <para>
    /// Azure AI Foundry supports two kinds of projects - Hub-based projects and non-Hub-based projects (also
    /// known simply as Foundry projects). See <see href="https://learn.microsoft.com/azure/ai-foundry/how-to/create-projects">Create a project for Azure AI Foundry</see>.
    /// </para>
    /// <para>
    /// Use this constructor overload if you are working with a non-Hub-based project.
    /// </para>
    /// </remarks>
    public ContentSafetyServiceConfiguration(
        TokenCredential credential,
        Uri endpoint,
        HttpClient? httpClient = null,
        int timeoutInSecondsForRetries = DefaultTimeoutInSecondsForRetries)
    {
        Credential = Throw.IfNull(credential);
        Endpoint = Throw.IfNull(endpoint);
        HttpClient = httpClient;
        TimeoutInSecondsForRetries = timeoutInSecondsForRetries;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSafetyServiceConfiguration"/> class for a non-Hub-based
    /// Azure AI Foundry project with the specified <paramref name="endpointUrl"/>.
    /// </summary>
    /// <param name="credential">
    /// The Azure <see cref="TokenCredential"/> that should be used when authenticating requests.
    /// </param>
    /// <param name="endpointUrl">
    /// The endpoint URL for the non-Hub-based Azure AI Foundry project.
    /// </param>
    /// <param name="httpClient">
    /// The <see cref="HttpClient"/> that should be used when communicating with the Azure AI Foundry Evaluation
    /// service. While the parameter is optional, it is recommended to supply an <see cref="HttpClient"/> that is
    /// configured with robust resilience and retry policies.
    /// </param>
    /// <param name="timeoutInSecondsForRetries">
    /// The timeout (in seconds) after which a <see cref="ContentSafetyEvaluator"/> should stop retrying failed
    /// attempts to communicate with the Azure AI Foundry Evaluation service when performing evaluations.
    /// </param>
    /// <remarks>
    /// <para>
    /// Azure AI Foundry supports two kinds of projects - Hub-based projects and non-Hub-based projects (also
    /// known simply as Foundry projects). See <see href="https://learn.microsoft.com/azure/ai-foundry/how-to/create-projects">Create a project for Azure AI Foundry</see>.
    /// </para>
    /// <para>
    /// Use this constructor overload if you are working with a non-Hub-based project.
    /// </para>
    /// </remarks>
    public ContentSafetyServiceConfiguration(
        TokenCredential credential,
        string endpointUrl,
        HttpClient? httpClient = null,
        int timeoutInSecondsForRetries = DefaultTimeoutInSecondsForRetries)
        : this(
              credential,
              endpoint: new Uri(Throw.IfNullOrWhitespace(endpointUrl)),
              httpClient,
              timeoutInSecondsForRetries)
    {
    }
}
