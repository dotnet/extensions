// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Middlewares.Internal;

/// <summary>
/// An <see cref="IMessageMiddleware"/> implementation to record latency for <see cref="MessageDelegate"/>.
/// </summary>
internal sealed class LatencyRecorderMiddleware : IMessageMiddleware
{
    private const string NoLatencyContextOnMessageContext = $"No {nameof(ILatencyContext)} is assigned to the {nameof(MessageContext)}";
    private const string NotAvailable = "N/A";

    private readonly MeasureToken _successMeasureToken;
    private readonly MeasureToken _failureMeasureToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatencyRecorderMiddleware"/> class.
    /// </summary>
    /// <param name="successMeasureToken">Success <see cref="MeasureToken"/>.</param>
    /// <param name="failureMeasureToken">Failed <see cref="MeasureToken"/>.</param>
    public LatencyRecorderMiddleware(MeasureToken successMeasureToken, MeasureToken failureMeasureToken)
    {
        _successMeasureToken = successMeasureToken;
        _failureMeasureToken = failureMeasureToken;
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(nextHandler);

        _ = context.TryGetLatencyContext(out ILatencyContext? latencyContext);
        if (latencyContext == null)
        {
            throw new InvalidOperationException(NoLatencyContextOnMessageContext);
        }

        Exception? exception = null;
        long startTimestamp = TimeProvider.System.GetTimestamp();
        try
        {
            await nextHandler.Invoke(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            long elapsed = TimeProvider.System.GetTimestamp() - startTimestamp;
            long ms = (long)Math.Round(((double)elapsed / TimeProvider.System.TimestampFrequency) * 1000);

            if (exception != null)
            {
                latencyContext.AddMeasure(_failureMeasureToken, ms);
                latencyContext.SetTag(new TagToken($"{_failureMeasureToken.Name}_Exception_Message", _failureMeasureToken.Position), exception.Message);
                latencyContext.SetTag(new TagToken($"{_failureMeasureToken.Name}_Exception_Class", _failureMeasureToken.Position), exception.GetType().FullName ?? NotAvailable);
            }
            else
            {
                latencyContext.AddMeasure(_successMeasureToken, ms);
            }
        }
    }
}
