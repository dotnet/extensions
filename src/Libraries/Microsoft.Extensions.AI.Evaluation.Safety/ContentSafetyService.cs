﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed partial class ContentSafetyService(
    ContentSafetyServiceConfiguration serviceConfiguration,
    string annotationTask,
    string evaluatorName)
{
    private static HttpClient? _sharedHttpClient;
    private static HttpClient SharedHttpClient
    {
        get
        {
            _sharedHttpClient ??= new HttpClient();
            return _sharedHttpClient;
        }
    }

    private static readonly ConcurrentDictionary<ContentSafetyServiceConfiguration, string> _serviceUrlCache =
        new ConcurrentDictionary<ContentSafetyServiceConfiguration, string>(UrlConfigurationComparer.Instance);

    private readonly HttpClient _httpClient = serviceConfiguration.HttpClient ?? SharedHttpClient;

    private string? _serviceUrl;

    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        IEnumerable<string?>? contexts = null,
        ContentSafetyServicePayloadFormat payloadFormat = ContentSafetyServicePayloadFormat.HumanSystem,
        IEnumerable<string>? metricNames = null,
        CancellationToken cancellationToken = default)
    {
        JsonObject payload;
        IList<EvaluationDiagnostic>? diagnostics;
        string annotationResult;
        string duration;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            string serviceUrl = await GetServiceUrlAsync(cancellationToken).ConfigureAwait(false);

            (payload, diagnostics) =
                ContentSafetyServicePayloadUtilities.GetPayload(
                    payloadFormat,
                    messages,
                    modelResponse,
                    annotationTask,
                    evaluatorName,
                    contexts,
                    metricNames,
                    cancellationToken);

            string resultUrl =
                await SubmitAnnotationRequestAsync(serviceUrl, payload, cancellationToken).ConfigureAwait(false);

            annotationResult = await FetchAnnotationResultAsync(resultUrl, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            duration = $"{stopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} s";
        }

        EvaluationResult result = ParseAnnotationResult(annotationResult, duration);

        if (diagnostics is not null)
        {
            result.AddDiagnosticsToAllMetrics(diagnostics);
        }

#pragma warning disable S125 // Sections of code should not be commented out
        // The following commented code can be useful for debugging purposes.
        // result.AddDiagnosticsToAllMetrics(
        //     EvaluationDiagnostic.Informational(
        //         $"""
        //         Annotation Request Payload:
        //         {payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}
        //
        //         Annotation Result:
        //         {annotationResult}
        //         """));
#pragma warning restore S125

        return result;
    }

    private static EvaluationResult ParseAnnotationResult(string annotationResponse, string evaluationDuration)
    {
#pragma warning disable S125 // Sections of code should not be commented out
        // Example annotation response:
        // [
        //   {
        //     "violence": "{\"label\":0,\"reasoning\":\"The system's response does not contain any violent content.\",\"version\":\"0.4\"}",
        //     "self_harm": "{\"label\":0,\"reasoning\":\"The system's response and does not contain any self-harm-related content.\",\"version\":\"0.4\"}",
        //     "hate_fairness": "{\"label\":0,\"reasoning\":\"The system's response contains no negative sentiment or hateful and unfair content.\",\"version\":\"0.4\"}",
        //   }
        //   {
        //     "violence": "{\"label\":0,\"reasoning\":\"The system's response provides factual information with no violent content.\",\"version\":\"0.4\"}",
        //     "self_harm": "{\"label\":0,\"reasoning\":\"The system's response provides factual information with no self-harm-related content.\",\"version\":\"0.4\"}",
        //     "hate_fairness": "{\"label\":0,\"reasoning\":\"The system's response does not contain any negative sentiment or hateful and unfair content.\",\"version\":\"0.4\"}",
        //   }
        // ]
#pragma warning restore S125

        EvaluationResult result = new EvaluationResult();

        using JsonDocument annotationResponseDocument = JsonDocument.Parse(annotationResponse);

        // If annotation results for multiple conversation turns are present in the response, we ignore all but the
        // last result since we want to report evaluation scores for the final turn in the conversation only.
        JsonElement metricElement = annotationResponseDocument.RootElement.EnumerateArray().Last();

        foreach (JsonProperty metricProperty in metricElement.EnumerateObject())
        {
            string metricName = metricProperty.Name;
            string metricDetails = metricProperty.Value.GetString()!;

            using JsonDocument metricDetailsDocument = JsonDocument.Parse(metricDetails);
            JsonElement metricDetailsRootElement = metricDetailsDocument.RootElement;

            JsonElement labelElement = metricDetailsRootElement.GetProperty("label");
            string? reason = metricDetailsRootElement.GetProperty("reasoning").GetString();

            EvaluationMetric metric;
            switch (labelElement.ValueKind)
            {
                case JsonValueKind.Number:
                    double doubleValue = labelElement.GetDouble();
                    metric = new NumericMetric(metricName, doubleValue, reason);
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    bool booleanValue = labelElement.GetBoolean();
                    metric = new BooleanMetric(metricName, booleanValue, reason);
                    break;

                case JsonValueKind.String:
                    string stringValue = labelElement.GetString()!;
                    if (double.TryParse(stringValue, out doubleValue))
                    {
                        metric = new NumericMetric(metricName, doubleValue, reason);
                    }
                    else if (bool.TryParse(stringValue, out booleanValue))
                    {
                        metric = new BooleanMetric(metricName, booleanValue, reason);
                    }
                    else
                    {
                        metric = new StringMetric(metricName, stringValue, reason);
                    }

                    break;

                default:
                    metric = new StringMetric(metricName, labelElement.ToString(), reason);
                    break;
            }

            foreach (JsonProperty property in metricDetailsRootElement.EnumerateObject())
            {
                if (property.Name != "label" && property.Name != "reasoning")
                {
                    metric.AddOrUpdateMetadata(property.Name, property.Value.ToString());
                }
            }

            metric.AddOrUpdateMetadata("evaluation-duration", evaluationDuration);

            result.Metrics[metric.Name] = metric;
        }

        return result;
    }

    private async ValueTask<string> GetServiceUrlAsync(CancellationToken cancellationToken)
    {
        if (_serviceUrl is not null)
        {
            return _serviceUrl;
        }

        if (_serviceUrlCache.TryGetValue(serviceConfiguration, out string? serviceUrl))
        {
            _serviceUrl = serviceUrl;
            return _serviceUrl;
        }

        string discoveryUrl = await GetServiceDiscoveryUrlAsync(cancellationToken).ConfigureAwait(false);

        serviceUrl =
            $"{discoveryUrl}/raisvc/v1.0" +
            $"/subscriptions/{serviceConfiguration.SubscriptionId}" +
            $"/resourceGroups/{serviceConfiguration.ResourceGroupName}" +
            $"/providers/Microsoft.MachineLearningServices/workspaces/{serviceConfiguration.ProjectName}";

        await EnsureServiceAvailabilityAsync(
            serviceUrl,
            annotationTask,
            cancellationToken).ConfigureAwait(false);

        _ = _serviceUrlCache.TryAdd(serviceConfiguration, serviceUrl);
        _serviceUrl = serviceUrl;
        return _serviceUrl;
    }

    private async ValueTask<string> GetServiceDiscoveryUrlAsync(CancellationToken cancellationToken)
    {
        string requestUrl =
            $"https://management.azure.com/subscriptions/{serviceConfiguration.SubscriptionId}" +
            $"/resourceGroups/{serviceConfiguration.ResourceGroupName}" +
            $"/providers/Microsoft.MachineLearningServices/workspaces/{serviceConfiguration.ProjectName}" +
            $"?api-version=2023-08-01-preview";

        HttpResponseMessage response =
            await GetResponseAsync(requestUrl, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"""
                {evaluatorName} failed to retrieve discovery URL for Azure AI Content Safety service.
                {response.StatusCode} ({(int)response.StatusCode}): {response.ReasonPhrase}.
                To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.
                """);
        }

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        using JsonDocument document = JsonDocument.Parse(responseContent);
        string? discoveryUrl = document.RootElement.GetProperty("properties").GetProperty("discoveryUrl").GetString();
        if (string.IsNullOrWhiteSpace(discoveryUrl))
        {
            throw new InvalidOperationException(
                $"""
                {evaluatorName} failed to retrieve discovery URL from the Azure AI Content Safety service's response below.
                To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.

                {responseContent}
                """);
        }

        Uri discoveryUri = new Uri(discoveryUrl);
        return $"{discoveryUri.Scheme}://{discoveryUri.Host}";
    }

    private async ValueTask EnsureServiceAvailabilityAsync(
        string serviceUrl,
        string capability,
        CancellationToken cancellationToken)
    {
        string serviceAvailabilityUrl = $"{serviceUrl}/checkannotation";

        HttpResponseMessage response =
            await GetResponseAsync(serviceAvailabilityUrl, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"""
                {evaluatorName} failed to check service availability for the Azure AI Content Safety service.
                The service is either unavailable in this region, or you lack the necessary permissions to access the AI project.
                {response.StatusCode} ({(int)response.StatusCode}): {response.ReasonPhrase}.
                To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.
                """);
        }

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        using JsonDocument document = JsonDocument.Parse(responseContent);
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string? supportedCapability = element.GetString();
            if (!string.IsNullOrWhiteSpace(supportedCapability) &&
                string.Equals(supportedCapability, capability, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"""
            The required {nameof(capability)} '{capability}' required for {evaluatorName} is not supported by the Azure AI Content Safety service in this region.
            To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.

            The following response identifies the capabilities that are supported:
            {responseContent}
            """);
    }

    private async ValueTask<string> SubmitAnnotationRequestAsync(
        string serviceUrl,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        string annotationUrl = $"{serviceUrl}/submitannotation";
        string payloadString = payload.ToJsonString();

        HttpResponseMessage response =
            await GetResponseAsync(
                annotationUrl,
                requestMethod: HttpMethod.Post,
                payloadString,
                cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"""
                {evaluatorName} failed to submit annotation request to the Azure AI Content Safety service.
                {response.StatusCode} ({(int)response.StatusCode}): {response.ReasonPhrase}.
                To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.
                """);
        }

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        using JsonDocument document = JsonDocument.Parse(responseContent);
        string? resultUrl = document.RootElement.GetProperty("location").GetString();

        if (string.IsNullOrWhiteSpace(resultUrl))
        {
            throw new InvalidOperationException(
                $"""
                {evaluatorName} failed to retrieve the result location from the following response for the annotation request submitted to The Azure AI Content Safety service.

                {responseContent}
                """);
        }

        return resultUrl!;
    }

    private async ValueTask<string> FetchAnnotationResultAsync(
        string resultUrl,
        CancellationToken cancellationToken)
    {
        const int InitialDelayInMilliseconds = 500;

        int attempts = 0;
        HttpResponseMessage response;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            do
            {
                ++attempts;
                response = await GetResponseAsync(resultUrl, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    TimeSpan elapsedDuration = stopwatch.Elapsed;
                    if (elapsedDuration.TotalSeconds >= serviceConfiguration.TimeoutInSecondsForRetries)
                    {
                        throw new InvalidOperationException(
                            $"""
                            {evaluatorName} failed to retrieve annotation result from the Azure AI Content Safety service.
                            The evaluation was timed out after {elapsedDuration} seconds (and {attempts} attempts).
                            {response.StatusCode} ({(int)response.StatusCode}): {response.ReasonPhrase}.
                            """);
                    }
                    else
                    {
#pragma warning disable EA0002 // Use 'System.TimeProvider' to make the code easier to test
                        await Task.Delay(InitialDelayInMilliseconds * attempts, cancellationToken).ConfigureAwait(false);
#pragma warning restore EA0002
                    }
                }
            }
            while (response.StatusCode != HttpStatusCode.OK);
        }
        finally
        {
            stopwatch.Stop();
        }

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        return responseContent;
    }

    private async ValueTask<HttpResponseMessage> GetResponseAsync(
        string requestUrl,
        HttpMethod? requestMethod = null,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        requestMethod ??= HttpMethod.Get;
        using var request = new HttpRequestMessage(requestMethod, requestUrl);

        request.Content = new StringContent(payload ?? string.Empty);
        await AddHeadersAsync(request, cancellationToken).ConfigureAwait(false);

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response;
    }

    private async ValueTask AddHeadersAsync(
        HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken = default)
    {
        string userAgent =
            $"microsoft-extensions-ai-evaluation/{Constants.Version} (type=evaluator; subtype={evaluatorName})";

        httpRequestMessage.Headers.Add("User-Agent", userAgent);

        AccessToken token =
            await serviceConfiguration.Credential.GetTokenAsync(
                new TokenRequestContext(scopes: ["https://management.azure.com/.default"]),
                cancellationToken).ConfigureAwait(false);

        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        if (httpRequestMessage.Content is not null)
        {
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
    }
}
