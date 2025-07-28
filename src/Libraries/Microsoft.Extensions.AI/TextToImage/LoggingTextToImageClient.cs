// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating text to image client that logs text to image operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="ITextToImageClient"/> is thread-safe for concurrent use so long as the
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
public partial class LoggingTextToImageClient : DelegatingTextToImageClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingTextToImageClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="ITextToImageClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public LoggingTextToImageClient(ITextToImageClient innerClient, ILogger logger)
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
    public override async Task<TextToImageResponse> GenerateImagesAsync(
        string prompt, TextToImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(GenerateImagesAsync), prompt, AsJson(options), AsJson(this.GetService<TextToImageClientMetadata>()));
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
    public override async Task<TextToImageResponse> EditImageAsync(
        AIContent originalImage, string prompt, TextToImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(EditImageAsync), prompt, AsJson(options), AsJson(this.GetService<TextToImageClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(EditImageAsync));
            }
        }

        try
        {
            var response = await base.EditImageAsync(originalImage, prompt, options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(EditImageAsync), AsJson(response));
                }
                else
                {
                    LogCompleted(nameof(EditImageAsync));
                }
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(EditImageAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(EditImageAsync), ex);
            throw;
        }
    }

    private string AsJson<T>(T value) => LoggingHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: Prompt: {Prompt}. Options: {TextToImageOptions}. Metadata: {TextToImageClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string prompt, string textToImageOptions, string textToImageClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {TextToImageResponse}.")]
    private partial void LogCompletedSensitive(string methodName, string textToImageResponse);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
