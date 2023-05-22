// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Builder instance which chains policies returning a policy wrap to be registered under unique key.
/// </summary>
/// <seealso cref="IPolicyPipelineBuilder" />
internal sealed class PolicyPipelineBuilder : IPolicyPipelineBuilder
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IPipelineMetering _metering;
    private readonly ILogger<PipelineTelemetry> _logger;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private PipelineId? _pipelineId;

    internal List<IAsyncPolicy> Policies { get; } = new();

    public PolicyPipelineBuilder(IPolicyFactory policyFactory, IPipelineMetering metering, ILogger<PipelineTelemetry> logger)
    {
        _policyFactory = policyFactory;
        _metering = metering;
        _logger = logger;
    }

    public void Initialize(PipelineId pipelineId)
    {
        _pipelineId = pipelineId;
        _policyFactory.Initialize(pipelineId);
        _metering.Initialize(pipelineId);
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder AddCircuitBreakerPolicy(
        string policyName,
        CircuitBreakerPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateCircuitBreakerPolicy(policyName, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder AddRetryPolicy(string policyName, RetryPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateRetryPolicy(policyName, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder AddTimeoutPolicy(string policyName, TimeoutPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateTimeoutPolicy(policyName, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder AddBulkheadPolicy(string policyName, BulkheadPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateBulkheadPolicy(policyName, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder AddFallbackPolicy(
        string policyName,
        FallbackScenarioTaskProvider provider,
        FallbackPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(provider);

        return AddPolicy(_policyFactory.CreateFallbackPolicy(policyName, provider, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder AddHedgingPolicy(
        string policyName,
        HedgedTaskProvider provider,
        HedgingPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(provider);

        return AddPolicy(_policyFactory.CreateHedgingPolicy(policyName, provider, options));
    }

    public IPolicyPipelineBuilder AddPolicy(IAsyncPolicy policy)
    {
        Policies.Add(policy);

        return this;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">At least one policy must be configured.</exception>
    public IAsyncPolicy Build()
    {
        if (Policies.Count == 0)
        {
            Throw.InvalidOperationException("At least one policy must be configured.");
        }

        var policy = Policies.Count > 1 ?
            new AsyncPolicyPipeline(Policies) :
            Policies[0];

        if (_pipelineId != null)
        {
            policy = PipelineTelemetry.Create(_pipelineId, policy, _metering, _logger, _timeProvider).WithPolicyKey(_pipelineId.PolicyPipelineKey);
        }

        return policy;
    }
}
