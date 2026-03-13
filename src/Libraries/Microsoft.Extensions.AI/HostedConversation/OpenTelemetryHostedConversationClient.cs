// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating hosted conversation client that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.40, defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class OpenTelemetryHostedConversationClient : DelegatingHostedConversationClient
{
    private const string HostedConversationCreateName = "hosted_conversation create";
    private const string HostedConversationGetName = "hosted_conversation get";
    private const string HostedConversationDeleteName = "hosted_conversation delete";
    private const string HostedConversationAddMessagesName = "hosted_conversation add_messages";
    private const string HostedConversationGetMessagesName = "hosted_conversation get_messages";

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _providerName;
    private readonly string? _serverAddress;
    private readonly int _serverPort;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryHostedConversationClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IHostedConversationClient"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting any logging data from the client.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
#pragma warning disable IDE0060 // Remove unused parameter; it exists for consistency with other OTel clients and future use
    public OpenTelemetryHostedConversationClient(IHostedConversationClient innerClient, ILogger? logger = null, string? sourceName = null)
#pragma warning restore IDE0060
        : base(innerClient)
    {
        Debug.Assert(innerClient is not null, "Should have been validated by the base ctor");

        if (innerClient!.GetService<HostedConversationClientMetadata>() is HostedConversationClientMetadata metadata)
        {
            _providerName = metadata.ProviderName;
            _serverAddress = metadata.ProviderUri?.Host;
            _serverPort = metadata.ProviderUri?.Port ?? 0;
        }

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description,
            advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries }
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
    public bool EnableSensitiveData { get; set; } = TelemetryHelpers.EnableSensitiveDataDefault;

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource) ? _activitySource :
        base.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public override async Task<HostedConversation> CreateAsync(
        HostedConversationCreationOptions? options = null, CancellationToken cancellationToken = default)
    {
        using Activity? activity = CreateAndConfigureActivity(HostedConversationCreateName);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

        Exception? error = null;
        try
        {
            return await base.CreateAsync(options, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, HostedConversationCreateName, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async Task<HostedConversation> GetAsync(
        string conversationId, CancellationToken cancellationToken = default)
    {
        using Activity? activity = CreateAndConfigureActivity(HostedConversationGetName, conversationId);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

        Exception? error = null;
        try
        {
            return await base.GetAsync(conversationId, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, HostedConversationGetName, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(
        string conversationId, CancellationToken cancellationToken = default)
    {
        using Activity? activity = CreateAndConfigureActivity(HostedConversationDeleteName, conversationId);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

        Exception? error = null;
        try
        {
            await base.DeleteAsync(conversationId, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, HostedConversationDeleteName, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async Task AddMessagesAsync(
        string conversationId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        using Activity? activity = CreateAndConfigureActivity(HostedConversationAddMessagesName, conversationId);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

        Exception? error = null;
        try
        {
            await base.AddMessagesAsync(conversationId, messages, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, HostedConversationAddMessagesName, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatMessage> GetMessagesAsync(
        string conversationId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using Activity? activity = CreateAndConfigureActivity(HostedConversationGetMessagesName, conversationId);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

        IAsyncEnumerable<ChatMessage> messages;
        try
        {
            messages = base.GetMessagesAsync(conversationId, cancellationToken);
        }
        catch (Exception ex)
        {
            TraceResponse(activity, HostedConversationGetMessagesName, ex, stopwatch);
            throw;
        }

        var enumerator = messages.GetAsyncEnumerator(cancellationToken);
        Exception? error = null;
        try
        {
            while (true)
            {
                ChatMessage message;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    message = enumerator.Current;
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }

                yield return message;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }
        }
        finally
        {
            TraceResponse(activity, HostedConversationGetMessagesName, error, stopwatch);

            await enumerator.DisposeAsync();
        }
    }

    /// <summary>Creates an activity for a hosted conversation operation, or returns <see langword="null"/> if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(string operationName, string? conversationId = null)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            activity = _activitySource.StartActivity(operationName, ActivityKind.Client);

            if (activity is { IsAllDataRequested: true })
            {
                _ = activity
                    .AddTag(OpenTelemetryConsts.GenAI.Operation.Name, operationName)
                    .AddTag(OpenTelemetryConsts.GenAI.Provider.Name, _providerName);

                if (conversationId is not null)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Conversation.Id, conversationId);
                }

                if (_serverAddress is not null)
                {
                    _ = activity
                        .AddTag(OpenTelemetryConsts.Server.Address, _serverAddress)
                        .AddTag(OpenTelemetryConsts.Server.Port, _serverPort);
                }
            }
        }

        return activity;
    }

    /// <summary>Records response information to the activity and metrics.</summary>
    private void TraceResponse(
        Activity? activity,
        string operationName,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;
            tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, operationName);
            tags.Add(OpenTelemetryConsts.GenAI.Provider.Name, _providerName);

            if (_serverAddress is string endpointAddress)
            {
                tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
                tags.Add(OpenTelemetryConsts.Server.Port, _serverPort);
            }

            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (error is not null)
        {
            _ = activity?
                .AddTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
        }
    }
}
