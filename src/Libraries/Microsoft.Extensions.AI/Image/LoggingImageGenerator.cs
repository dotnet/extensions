// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating image generator that logs image generation operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IImageGenerator"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// prompts and options are logged. These prompts and options may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Prompts and options are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental(diagnosticId: DiagnosticIds.Experiments.ImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public partial class LoggingImageGenerator : DelegatingImageGenerator
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingImageGenerator"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IImageGenerator"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public LoggingImageGenerator(IImageGenerator innerGenerator, ILogger logger)
        : base(innerGenerator)
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
    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request, ImageGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(request);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(GenerateAsync), request.Prompt ?? string.Empty, AsJson(options), AsJson(this.GetService<ImageGeneratorMetadata>()));
            }
            else
            {
                LogInvoked(nameof(GenerateAsync));
            }
        }

        try
        {
            var response = await base.GenerateAsync(request, options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace) && response.Contents.All(c => c is not DataContent))
                {
                    LogCompletedSensitive(nameof(GenerateAsync), AsJson(response));
                }
                else
                {
                    LogCompleted(nameof(GenerateAsync));
                }
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GenerateAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GenerateAsync), ex);
            throw;
        }
    }

    private string AsJson<T>(T value) => TelemetryHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: Prompt: {Prompt}. Options: {ImageGenerationOptions}. Metadata: {ImageGeneratorMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string prompt, string imageGenerationOptions, string imageGeneratorMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {ImageGenerationResponse}.")]
    private partial void LogCompletedSensitive(string methodName, string imageGenerationResponse);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
