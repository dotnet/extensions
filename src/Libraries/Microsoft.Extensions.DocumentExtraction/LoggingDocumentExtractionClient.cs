// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Logs extraction lifecycle events without logging source content, evidence values, artifacts, or provider state.
/// </summary>
public partial class LoggingDocumentExtractionClient : DelegatingDocumentExtractionClient
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingDocumentExtractionClient"/> class.
    /// </summary>
    public LoggingDocumentExtractionClient(IDocumentExtractionClient innerClient, ILogger logger)
        : base(innerClient)
    {
        _logger = Throw.IfNull(logger);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<DocumentExtractionPage> ExtractPagesAsync(
        System.IO.Stream document,
        string identifier,
        string mediaType,
        DocumentExtractionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogInvoked();

        IAsyncEnumerator<DocumentExtractionPage>? enumerator = null;
        try
        {
            enumerator = base.ExtractPagesAsync(document, identifier, mediaType, options, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogCanceled();
            throw;
        }
        catch (Exception)
        {
            LogFailed();
            throw;
        }

        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    LogCanceled();
                    throw;
                }
                catch (Exception)
                {
                    LogFailed();
                    throw;
                }

                if (!hasNext)
                {
                    break;
                }

                DocumentExtractionPage page = enumerator.Current;
                LogPageReceived(page.PageNumber, page.Observations.Count);
                yield return page;
            }

            LogCompleted();
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Document extraction invoked.")]
    private partial void LogInvoked();

    [LoggerMessage(LogLevel.Debug, "Document extraction received page {PageNumber} with {ObservationCount} observations.")]
    private partial void LogPageReceived(int pageNumber, int observationCount);

    [LoggerMessage(LogLevel.Debug, "Document extraction completed.")]
    private partial void LogCompleted();

    [LoggerMessage(LogLevel.Debug, "Document extraction canceled.")]
    private partial void LogCanceled();

    [LoggerMessage(LogLevel.Error, "Document extraction failed.")]
    private partial void LogFailed();
}
