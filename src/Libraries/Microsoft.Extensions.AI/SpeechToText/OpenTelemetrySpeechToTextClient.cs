﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating speech-to-text client that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.37, defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </remarks>
[Experimental("MEAI001")]
public sealed class OpenTelemetrySpeechToTextClient : DelegatingSpeechToTextClient
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    private readonly Histogram<int> _tokenUsageHistogram;
    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _defaultModelId;
    private readonly string? _providerName;
    private readonly string? _serverAddress;
    private readonly int _serverPort;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetrySpeechToTextClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="ISpeechToTextClient"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting any logging data from the client.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
#pragma warning disable IDE0060 // Remove unused parameter; it exists for consistency with IChatClient and future use
    public OpenTelemetrySpeechToTextClient(ISpeechToTextClient innerClient, ILogger? logger = null, string? sourceName = null)
#pragma warning restore IDE0060
        : base(innerClient)
    {
        Debug.Assert(innerClient is not null, "Should have been validated by the base ctor");

        if (innerClient!.GetService<SpeechToTextClientMetadata>() is SpeechToTextClientMetadata metadata)
        {
            _defaultModelId = metadata.DefaultModelId;
            _providerName = metadata.ProviderName;
            _serverAddress = metadata.ProviderUri?.Host;
            _serverPort = metadata.ProviderUri?.Port ?? 0;
        }

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _tokenUsageHistogram = _meter.CreateHistogram<int>(
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Name,
            OpenTelemetryConsts.TokensUnit,
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.TokenUsage.ExplicitBucketBoundaries }
#endif
            );

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries }
#endif
            );
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activitySource.Dispose();
            _meter.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Gets or sets a value indicating whether potentially sensitive information should be included in telemetry.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if potentially sensitive information should be included in telemetry;
    /// <see langword="false"/> if telemetry shouldn't include raw inputs and outputs.
    /// The default value is <see langword="false"/>, unless the <c>OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT</c>
    /// environment variable is set to "true" (case-insensitive).
    /// </value>
    /// <remarks>
    /// By default, telemetry includes metadata, such as token counts, but not raw inputs
    /// and outputs, such as message content, function call arguments, and function call results.
    /// The default value can be overridden by setting the <c>OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT</c>
    /// environment variable to "true". Explicitly setting this property will override the environment variable.
    /// </remarks>
    public bool EnableSensitiveData { get; set; } = TelemetryHelpers.EnableSensitiveDataDefault;

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource) ? _activitySource :
        base.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public override async Task<SpeechToTextResponse> GetTextAsync(Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        SpeechToTextResponse? response = null;
        Exception? error = null;
        try
        {
            response = await base.GetTextAsync(audioSpeechStream, options, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, requestModelId, response, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        IAsyncEnumerable<SpeechToTextResponseUpdate> updates;
        try
        {
            updates = base.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken);
        }
        catch (Exception ex)
        {
            TraceResponse(activity, requestModelId, response: null, ex, stopwatch);
            throw;
        }

        var responseEnumerator = updates.GetAsyncEnumerator(cancellationToken);
        List<SpeechToTextResponseUpdate> trackedUpdates = [];
        Exception? error = null;
        try
        {
            while (true)
            {
                SpeechToTextResponseUpdate update;
                try
                {
                    if (!await responseEnumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = responseEnumerator.Current;
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }

                trackedUpdates.Add(update);
                yield return update;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }
        }
        finally
        {
            TraceResponse(activity, requestModelId, trackedUpdates.ToSpeechToTextResponse(), error, stopwatch);

            await responseEnumerator.DisposeAsync();
        }
    }

    /// <summary>Creates an activity for a speech-to-text request, or returns <see langword="null"/> if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(SpeechToTextOptions? options)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            string? modelId = options?.ModelId ?? _defaultModelId;

            activity = _activitySource.StartActivity(
                string.IsNullOrWhiteSpace(modelId) ? OpenTelemetryConsts.GenAI.GenerateContentName : $"{OpenTelemetryConsts.GenAI.GenerateContentName} {modelId}",
                ActivityKind.Client);

            if (activity is { IsAllDataRequested: true })
            {
                _ = activity
                    .AddTag(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.GenerateContentName)
                    .AddTag(OpenTelemetryConsts.GenAI.Request.Model, modelId)
                    .AddTag(OpenTelemetryConsts.GenAI.Provider.Name, _providerName)
                    .AddTag(OpenTelemetryConsts.GenAI.Output.Type, OpenTelemetryConsts.TypeText);

                if (_serverAddress is not null)
                {
                    _ = activity
                        .AddTag(OpenTelemetryConsts.Server.Address, _serverAddress)
                        .AddTag(OpenTelemetryConsts.Server.Port, _serverPort);
                }

                if (options is not null)
                {
                    if (EnableSensitiveData)
                    {
                        // Log all additional request options as raw values on the span.
                        // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data.
                        if (options.AdditionalProperties is { } props)
                        {
                            foreach (KeyValuePair<string, object?> prop in props)
                            {
                                _ = activity.AddTag(prop.Key, prop.Value);
                            }
                        }
                    }
                }
            }
        }

        return activity;
    }

    /// <summary>Adds speech-to-text response information to the activity.</summary>
    private void TraceResponse(
        Activity? activity,
        string? requestModelId,
        SpeechToTextResponse? response,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;

            AddMetricTags(ref tags, requestModelId, response);
            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (_tokenUsageHistogram.Enabled && response?.Usage is { } usage)
        {
            if (usage.InputTokenCount is long inputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeInput);
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)inputTokens, tags);
            }

            if (usage.OutputTokenCount is long outputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, OpenTelemetryConsts.TokenTypeOutput);
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)outputTokens, tags);
            }
        }

        if (error is not null)
        {
            _ = activity?
                .AddTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
        }

        if (response is not null)
        {
            AddOutputMessagesTags(response, activity);

            if (activity is not null)
            {
                if (!string.IsNullOrWhiteSpace(response.ResponseId))
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Id, response.ResponseId);
                }

                if (response.ModelId is not null)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Model, response.ModelId);
                }

                if (response.Usage?.InputTokenCount is long inputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputTokens, (int)inputTokens);
                }

                if (response.Usage?.OutputTokenCount is long outputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.OutputTokens, (int)outputTokens);
                }

                // Log all additional response properties as raw values on the span.
                // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data.
                if (EnableSensitiveData && response.AdditionalProperties is { } props)
                {
                    foreach (KeyValuePair<string, object?> prop in props)
                    {
                        _ = activity.AddTag(prop.Key, prop.Value);
                    }
                }
            }
        }

        void AddMetricTags(ref TagList tags, string? requestModelId, SpeechToTextResponse? response)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.GenerateContentName);

            if (requestModelId is not null)
            {
                tags.Add(OpenTelemetryConsts.GenAI.Request.Model, requestModelId);
            }

            tags.Add(OpenTelemetryConsts.GenAI.Provider.Name, _providerName);

            if (_serverAddress is string endpointAddress)
            {
                tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
                tags.Add(OpenTelemetryConsts.Server.Port, _serverPort);
            }

            if (response?.ModelId is string responseModel)
            {
                tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModel);
            }
        }
    }

    private void AddOutputMessagesTags(SpeechToTextResponse response, Activity? activity)
    {
        if (EnableSensitiveData && activity is { IsAllDataRequested: true })
        {
            _ = activity.AddTag(
                OpenTelemetryConsts.GenAI.Output.Messages,
                OpenTelemetryChatClient.SerializeChatMessages([new(ChatRole.Assistant, response.Contents)]));
        }
    }
}
