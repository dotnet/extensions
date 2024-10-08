// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// The draft specification this follows is available at https://opentelemetry.io/docs/specs/semconv/gen-ai/.
/// The specification is still experimental and subject to change; as such, the telemetry output by this generator is also subject to change.
/// </remarks>
/// <typeparam name="TInput">The type of input used to produce embeddings.</typeparam>
/// <typeparam name="TEmbedding">The type of embedding generated.</typeparam>
public sealed class OpenTelemetryEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    private readonly Histogram<int> _tokenUsageHistogram;
    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _modelId;
    private readonly string? _modelProvider;
    private readonly string? _endpointAddress;
    private readonly int _endpointPort;
    private readonly int? _dimensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryEmbeddingGenerator{TInput, TEmbedding}"/> class.
    /// </summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>, which is the next stage of the pipeline.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    public OpenTelemetryEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, string? sourceName = null)
        : base(innerGenerator)
    {
        Debug.Assert(innerGenerator is not null, "Should have been validated by the base ctor.");

        EmbeddingGeneratorMetadata metadata = innerGenerator!.Metadata;
        _modelId = metadata.ModelId;
        _modelProvider = metadata.ProviderName;
        _endpointAddress = metadata.ProviderUri?.GetLeftPart(UriPartial.Path);
        _endpointPort = metadata.ProviderUri?.Port ?? 0;
        _dimensions = metadata.Dimensions;

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _tokenUsageHistogram = _meter.CreateHistogram<int>(
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Name,
            OpenTelemetryConsts.TokensUnit,
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Description,
            advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.TokenUsage.ExplicitBucketBoundaries });

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description,
            advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries });
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

    /// <summary>Gets a value indicating whether diagnostics are enabled.</summary>
    private bool Enabled => _activitySource.HasListeners();

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        using Activity? activity = StartActivity();
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

        GeneratedEmbeddings<TEmbedding>? response = null;
        Exception? error = null;
        try
        {
            response = await base.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            SetCompletionResponse(activity, response, error, stopwatch);
        }

        return response;
    }

    /// <summary>Creates an activity for an embedding generation request, or returns null if not enabled.</summary>
    private Activity? StartActivity()
    {
        Activity? activity = null;
        if (Enabled)
        {
            activity = _activitySource.StartActivity(
                $"embedding {_modelId}",
                ActivityKind.Client,
                default(ActivityContext),
                [
                    new(OpenTelemetryConsts.GenAI.Operation.Name, "embedding"),
                    new(OpenTelemetryConsts.GenAI.Request.Model, _modelId),
                    new(OpenTelemetryConsts.GenAI.System, _modelProvider),
                ]);

            if (activity is not null)
            {
                if (_endpointAddress is not null)
                {
                    _ = activity
                        .SetTag(OpenTelemetryConsts.Server.Address, _endpointAddress)
                        .SetTag(OpenTelemetryConsts.Server.Port, _endpointPort);
                }

                if (_dimensions is int dimensions)
                {
                    _ = activity.SetTag(OpenTelemetryConsts.GenAI.Request.EmbeddingDimensions, dimensions);
                }
            }
        }

        return activity;
    }

    /// <summary>Adds embedding generation response information to the activity.</summary>
    private void SetCompletionResponse(
        Activity? activity,
        GeneratedEmbeddings<TEmbedding>? embeddings,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (!Enabled)
        {
            return;
        }

        int? inputTokens = null;
        string? responseModelId = null;
        if (embeddings is not null)
        {
            responseModelId = embeddings.FirstOrDefault()?.ModelId;
            if (embeddings.Usage?.InputTokenCount is int i)
            {
                inputTokens = inputTokens.GetValueOrDefault() + i;
            }
        }

        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;
            AddMetricTags(ref tags, responseModelId);
            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (_tokenUsageHistogram.Enabled && inputTokens.HasValue)
        {
            TagList tags = default;
            tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "input");
            AddMetricTags(ref tags, responseModelId);

            _tokenUsageHistogram.Record(inputTokens.Value);
        }

        if (activity is null)
        {
            return;
        }

        if (error is not null)
        {
            _ = activity
                .SetTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
            return;
        }

        if (inputTokens.HasValue)
        {
            _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.InputTokens, inputTokens);
        }

        if (responseModelId is not null)
        {
            _ = activity.SetTag(OpenTelemetryConsts.GenAI.Response.Model, responseModelId);
        }
    }

    private void AddMetricTags(ref TagList tags, string? responseModelId)
    {
        tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, "embedding");

        if (_modelId is string requestModel)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Request.Model, requestModel);
        }

        tags.Add(OpenTelemetryConsts.GenAI.System, _modelProvider);

        if (_endpointAddress is string endpointAddress)
        {
            tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
            tags.Add(OpenTelemetryConsts.Server.Port, _endpointPort);
        }

        // Assume all of the embeddings in the same batch used the same model
        if (responseModelId is not null)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModelId);
        }
    }
}
