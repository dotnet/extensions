// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Resilience.Internal;
using Polly;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// The helper for pipeline telemetry.
/// </summary>
internal sealed class PipelineTelemetry
{
    public static IAsyncPolicy<T> Create<T>(
        PipelineId pipelineId,
        IAsyncPolicy<T> policy,
        IPipelineMetering metering,
        ILogger<PipelineTelemetry> logger,
        TimeProvider timeProvider) => new TelemetryPolicy<T>(pipelineId, policy, metering, logger, timeProvider);

    public static IAsyncPolicy Create(
        PipelineId pipelineId,
        IAsyncPolicy policy,
        IPipelineMetering metering,
        ILogger<PipelineTelemetry> logger,
        TimeProvider timeProvider) => new TelemetryPolicy(pipelineId, policy, metering, logger, timeProvider);

    internal sealed class TelemetryPolicy<T> : AsyncPolicy<T>
    {
        private readonly PipelineId _pipelineId;
        private readonly IAsyncPolicy<T> _policy;
        private readonly IPipelineMetering _metering;
        private readonly ILogger<PipelineTelemetry> _logger;
        private readonly TimeProvider _timeProvider;

        public TelemetryPolicy(PipelineId pipelineId, IAsyncPolicy<T> policy, IPipelineMetering metering, ILogger<PipelineTelemetry> logger, TimeProvider timeProvider)
        {
            _pipelineId = pipelineId;
            _policy = policy;
            _metering = metering;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        protected override async Task<T> ImplementationAsync(Func<Context, CancellationToken, Task<T>> action, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            var start = _timeProvider.GetTimestamp();

            try
            {
                _logger.ExecutingPipeline(_pipelineId.PipelineName, _pipelineId.PipelineKey.GetDimensionOrUnknown());

                var result = await _policy.ExecuteAsync(action, context, cancellationToken, continueOnCapturedContext).ConfigureAwait(false);

                _metering.RecordPipelineExecution(GetElapsedTime(start, _timeProvider), null, context);

                _logger.PipelineExecuted(_pipelineId.PipelineName, _pipelineId.PipelineKey.GetDimensionOrUnknown(), GetElapsedTime(start, _timeProvider));

                return result;
            }
            catch (Exception e)
            {
                _metering.RecordPipelineExecution(GetElapsedTime(start, _timeProvider), e, context);
                _logger.PipelineFailed(e, _pipelineId.PipelineName, _pipelineId.PipelineKey.GetDimensionOrUnknown(), GetElapsedTime(start, _timeProvider));

                throw;
            }
        }
    }

    internal static long GetElapsedTime(long startingTimestamp, TimeProvider timeProvider) => (long)timeProvider.GetElapsedTime(startingTimestamp, timeProvider.GetTimestamp()).TotalMilliseconds;

    internal sealed class TelemetryPolicy : AsyncPolicy
    {
        private readonly PipelineId _pipelineId;
        private readonly IAsyncPolicy _policy;
        private readonly IPipelineMetering _metering;
        private readonly ILogger<PipelineTelemetry> _logger;
        private readonly TimeProvider _timeProvider;

        public TelemetryPolicy(PipelineId pipelineId, IAsyncPolicy policy, IPipelineMetering metering, ILogger<PipelineTelemetry> logger, TimeProvider timeProvider)
        {
            _pipelineId = pipelineId;
            _policy = policy;
            _metering = metering;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        protected override async Task<T> ImplementationAsync<T>(Func<Context, CancellationToken, Task<T>> action, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            var start = _timeProvider.GetTimestamp();

            try
            {
                _logger.ExecutingPipeline(_pipelineId.PipelineName, _pipelineId.PipelineKey.GetDimensionOrUnknown());

                var result = await _policy.ExecuteAsync(action, context, cancellationToken, continueOnCapturedContext).ConfigureAwait(false);

                _metering.RecordPipelineExecution(GetElapsedTime(start, _timeProvider), null, context);

                _logger.PipelineExecuted(_pipelineId.PipelineName, _pipelineId.PipelineKey.GetDimensionOrUnknown(), GetElapsedTime(start, _timeProvider));

                return result;
            }
            catch (Exception e)
            {
                _metering.RecordPipelineExecution(GetElapsedTime(start, _timeProvider), e, context);
                _logger.PipelineFailed(e, _pipelineId.PipelineName, _pipelineId.PipelineKey.GetDimensionOrUnknown(), GetElapsedTime(start, _timeProvider));

                throw;
            }
        }
    }
}
