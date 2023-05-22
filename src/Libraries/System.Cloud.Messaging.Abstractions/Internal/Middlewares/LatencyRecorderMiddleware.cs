// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// An <see cref="IMessageMiddleware"/> implementation to record latency for <see cref="IMessageDelegate"/>.
/// </summary>
internal sealed class LatencyRecorderMiddleware : IMessageMiddleware
{
    internal TimeProvider TimeProvider = TimeProvider.System;

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
    public async ValueTask InvokeAsync(MessageContext context, IMessageDelegate nextHandler)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(nextHandler);

        _ = context.TryGetLatencyContext(out ILatencyContext? latencyContext);
        _ = Throw.IfNull(latencyContext);

        Exception? exception = null;
        var timestamp = TimeProvider.GetTimestamp();
        try
        {
            await nextHandler.InvokeAsync(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            long latency = (long)TimeProvider.GetElapsedTime(timestamp, TimeProvider.GetTimestamp()).TotalMilliseconds;

            if (exception != null)
            {
                latencyContext.AddMeasure(_failureMeasureToken, latency);
                latencyContext.SetTag(new TagToken($"{_failureMeasureToken.Name}_Exception_Message", _failureMeasureToken.Position), exception.Message);
                latencyContext.SetTag(new TagToken($"{_failureMeasureToken.Name}_Exception_Class", _failureMeasureToken.Position), exception.GetType()?.FullName ?? NotAvailable);
            }
            else
            {
                latencyContext.AddMeasure(_successMeasureToken, latency);
            }
        }
    }
}
