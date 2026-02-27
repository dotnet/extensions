// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating hosted file client that logs file operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IHostedFileClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// options and results are logged. These may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Options and results are not logged at other logging levels.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed partial class LoggingHostedFileClient : DelegatingHostedFileClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingHostedFileClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IHostedFileClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingHostedFileClient(IHostedFileClient innerClient, ILogger logger)
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
    public override async Task<HostedFile> UploadAsync(
        Stream content, string? mediaType = null, string? fileName = null, HostedFileUploadOptions? options = null, CancellationToken cancellationToken = default)
    {
        fileName ??= content is FileStream fs ? Path.GetFileName(fs.Name) : null;
        mediaType ??= fileName is not null ? MediaTypeMap.GetMediaType(fileName) : null;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogUploadInvokedSensitive(mediaType, fileName, AsJson(options), AsJson(this.GetService<HostedFileClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(UploadAsync));
            }
        }

        try
        {
            var result = await base.UploadAsync(content, mediaType, fileName, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(UploadAsync), AsJson(result));
                }
                else
                {
                    LogCompleted(nameof(UploadAsync));
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(UploadAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(UploadAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<HostedFileDownloadStream> DownloadAsync(
        string fileId, HostedFileDownloadOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogDownloadInvokedSensitive(fileId, AsJson(options), AsJson(this.GetService<HostedFileClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(DownloadAsync));
            }
        }

        try
        {
            var result = await base.DownloadAsync(fileId, options, cancellationToken).ConfigureAwait(false);

            LogCompleted(nameof(DownloadAsync));

            return result;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(DownloadAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(DownloadAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<HostedFile?> GetFileInfoAsync(
        string fileId, HostedFileGetOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogGetFileInfoInvokedSensitive(fileId, AsJson(options), AsJson(this.GetService<HostedFileClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(GetFileInfoAsync));
            }
        }

        try
        {
            var result = await base.GetFileInfoAsync(fileId, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(GetFileInfoAsync), AsJson(result));
                }
                else
                {
                    LogCompleted(nameof(GetFileInfoAsync));
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetFileInfoAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetFileInfoAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<HostedFile> ListFilesAsync(
        HostedFileListOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogListFilesInvokedSensitive(AsJson(options), AsJson(this.GetService<HostedFileClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(ListFilesAsync));
            }
        }

        IAsyncEnumerator<HostedFile> e;
        try
        {
            e = base.ListFilesAsync(options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(ListFilesAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(ListFilesAsync), ex);
            throw;
        }

        try
        {
            HostedFile? file = null;
            while (true)
            {
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    file = e.Current;
                }
                catch (OperationCanceledException)
                {
                    LogInvocationCanceled(nameof(ListFilesAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(ListFilesAsync), ex);
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogListItemSensitive(AsJson(file));
                }

                yield return file;
            }

            LogCompleted(nameof(ListFilesAsync));
        }
        finally
        {
            await e.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> DeleteAsync(
        string fileId, HostedFileDeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogDeleteInvokedSensitive(fileId, AsJson(options), AsJson(this.GetService<HostedFileClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(DeleteAsync));
            }
        }

        try
        {
            var result = await base.DeleteAsync(fileId, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(DeleteAsync), AsJson(result));
                }
                else
                {
                    LogCompleted(nameof(DeleteAsync));
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(DeleteAsync), ex);
            throw;
        }
    }

    private string AsJson<T>(T value) => TelemetryHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "UploadAsync invoked. MediaType: {MediaType}. FileName: {FileName}. Options: {HostedFileOptions}. Metadata: {HostedFileClientMetadata}.")]
    private partial void LogUploadInvokedSensitive(string? mediaType, string? fileName, string hostedFileOptions, string hostedFileClientMetadata);

    [LoggerMessage(LogLevel.Trace, "DownloadAsync invoked. FileId: {FileId}. Options: {HostedFileOptions}. Metadata: {HostedFileClientMetadata}.")]
    private partial void LogDownloadInvokedSensitive(string fileId, string hostedFileOptions, string hostedFileClientMetadata);

    [LoggerMessage(LogLevel.Trace, "GetFileInfoAsync invoked. FileId: {FileId}. Options: {HostedFileOptions}. Metadata: {HostedFileClientMetadata}.")]
    private partial void LogGetFileInfoInvokedSensitive(string fileId, string hostedFileOptions, string hostedFileClientMetadata);

    [LoggerMessage(LogLevel.Trace, "ListFilesAsync invoked. Options: {HostedFileOptions}. Metadata: {HostedFileClientMetadata}.")]
    private partial void LogListFilesInvokedSensitive(string hostedFileOptions, string hostedFileClientMetadata);

    [LoggerMessage(LogLevel.Trace, "DeleteAsync invoked. FileId: {FileId}. Options: {HostedFileOptions}. Metadata: {HostedFileClientMetadata}.")]
    private partial void LogDeleteInvokedSensitive(string fileId, string hostedFileOptions, string hostedFileClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {HostedFileResult}.")]
    private partial void LogCompletedSensitive(string methodName, string hostedFileResult);

    [LoggerMessage(LogLevel.Trace, "ListFilesAsync received item: {HostedFile}")]
    private partial void LogListItemSensitive(string hostedFile);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}
