// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating text to image client that logs text to image operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IImageClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// prompts and options are logged. These prompts and options may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Prompts and options are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public partial class LoggingImageClient : DelegatingImageClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingImageClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IImageClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public LoggingImageClient(IImageClient innerClient, ILogger logger)
        : base(innerClient)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
    /// <exception cref="ArgumentNullException">The value being set is <see langword="null"/>.</exception>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    public override async Task<ImageResponse> GenerateImagesAsync(
        string prompt, ImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(GenerateImagesAsync), prompt, AsJson(options), AsJson(this.GetService<ImageClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(GenerateImagesAsync));
            }
        }

        try
        {
            var response = await base.GenerateImagesAsync(prompt, options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(GenerateImagesAsync), AsJson(response));
                }
                else
                {
                    LogCompleted(nameof(GenerateImagesAsync));
                }
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GenerateImagesAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GenerateImagesAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<ImageResponse> EditImagesAsync(
        IEnumerable<AIContent> originalImages, string prompt, ImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(EditImagesAsync), prompt, AsJson(options), AsJson(this.GetService<ImageClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(EditImagesAsync));
            }
        }

        try
        {
            var response = await base.EditImagesAsync(originalImages, prompt, options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace) && response.Contents.All(c => c is not DataContent))
                {
                    LogCompletedSensitive(nameof(EditImagesAsync), AsJson(response));
                }
                else
                {
                    LogCompleted(nameof(EditImagesAsync));
                }
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(EditImagesAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(EditImagesAsync), ex);
            throw;
        }
    }

    private string AsJson<T>(T value) => LoggingHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: Prompt: {Prompt}. Options: {ImageOptions}. Metadata: {ImageClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string prompt, string imageOptions, string imageClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {ImageResponse}.")]
    private partial void LogCompletedSensitive(string methodName, string imageResponse);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
