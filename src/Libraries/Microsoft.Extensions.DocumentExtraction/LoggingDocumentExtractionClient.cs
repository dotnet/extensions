// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>A delegating OCR client that logs OCR operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IDocumentExtractionClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// options and results are logged. These may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Options and results are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public partial class LoggingDocumentExtractionClient : DelegatingDocumentExtractionClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingDocumentExtractionClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IDocumentExtractionClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingDocumentExtractionClient(IDocumentExtractionClient innerClient, ILogger logger)
        : base(innerClient)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    public override async Task<DocumentExtractionResult> ExtractAsync(
        Stream document,
        string mediaType,
        DocumentExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(ExtractAsync), mediaType, AsJson(options), AsJson(this.GetService<DocumentExtractionClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(ExtractAsync));
            }
        }

        try
        {
            var result = await base.ExtractAsync(document, mediaType, options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(ExtractAsync), AsJson(result));
                }
                else
                {
                    LogCompleted(nameof(ExtractAsync));
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(ExtractAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(ExtractAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<DocumentExtractionPageResult> ExtractPagesAsync(
        Stream document,
        string mediaType,
        DocumentExtractionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(ExtractPagesAsync), mediaType, AsJson(options), AsJson(this.GetService<DocumentExtractionClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(ExtractPagesAsync));
            }
        }

        IAsyncEnumerator<DocumentExtractionPageResult> e;
        try
        {
            e = base.ExtractPagesAsync(document, mediaType, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(ExtractPagesAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(ExtractPagesAsync), ex);
            throw;
        }

        try
        {
            DocumentExtractionPageResult? update = null;
            while (true)
            {
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        break;
                    }

                    update = e.Current;
                }
                catch (OperationCanceledException)
                {
                    LogInvocationCanceled(nameof(ExtractPagesAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(ExtractPagesAsync), ex);
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        LogStreamingUpdateSensitive(AsJson(update));
                    }
                    else
                    {
                        LogStreamingUpdate();
                    }
                }

                yield return update;
            }

            LogCompleted(nameof(ExtractPagesAsync));
        }
        finally
        {
            await e.DisposeAsync();
        }
    }

    private string AsJson<T>(T value) => TelemetryHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: MediaType: {MediaType}. Options: {DocumentExtractionOptions}. Metadata: {DocumentExtractionClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string mediaType, string documentExtractionOptions, string documentExtractionClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {DocumentExtractionResult}.")]
    private partial void LogCompletedSensitive(string methodName, string documentExtractionResult);

    [LoggerMessage(LogLevel.Debug, "ExtractPagesAsync received update.")]
    private partial void LogStreamingUpdate();

    [LoggerMessage(LogLevel.Trace, "ExtractPagesAsync received update: {DocumentExtractionPageResult}")]
    private partial void LogStreamingUpdateSensitive(string documentExtractionPageResult);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
