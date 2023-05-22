// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// Builder instance which chains policies returning a policy wrap to be registered under unique key.
/// </summary>
/// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
/// <seealso cref="IPolicyPipelineBuilder{TResult}" />
internal sealed class PolicyPipelineBuilder<TResult> : IPolicyPipelineBuilder<TResult>
{
    private readonly IPolicyFactory _policyFactory;
    private readonly IPipelineMetering _metering;
    private readonly ILogger<PipelineTelemetry> _logger;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private PipelineId? _pipelineId;

    internal List<IAsyncPolicy<TResult>> Policies { get; } = new();

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
    public IPolicyPipelineBuilder<TResult> AddCircuitBreakerPolicy(
        string policyName,
        CircuitBreakerPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateCircuitBreakerPolicy(policyName, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder<TResult> AddRetryPolicy(string policyName, RetryPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateRetryPolicy(policyName, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder<TResult> AddTimeoutPolicy(string policyName, TimeoutPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateTimeoutPolicy(policyName, options).AsAsyncPolicy<TResult>());
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder<TResult> AddBulkheadPolicy(string policyName, BulkheadPolicyOptions options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);

        return AddPolicy(_policyFactory.CreateBulkheadPolicy(policyName, options).AsAsyncPolicy<TResult>());
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder<TResult> AddFallbackPolicy(
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        FallbackPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(provider);

        return AddPolicy(_policyFactory.CreateFallbackPolicy(policyName, provider, options));
    }

    /// <inheritdoc/>
    public IPolicyPipelineBuilder<TResult> AddHedgingPolicy(
        string policyName,
        HedgedTaskProvider<TResult> provider,
        HedgingPolicyOptions<TResult> options)
    {
        _ = Throw.IfNullOrEmpty(policyName);
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(provider);

        return AddPolicy(_policyFactory.CreateHedgingPolicy(policyName, provider, options));
    }

    public IPolicyPipelineBuilder<TResult> AddPolicy(IAsyncPolicy<TResult> policy)
    {
        Policies.Add(policy);

        return this;
    }

    public IPolicyPipelineBuilder<TResult> AddPolicy(IAsyncPolicy policy)
    {
        Policies.Add(policy.AsAsyncPolicy<TResult>());

        return this;
    }

    /// <inheritdoc cref="IPolicyPipelineBuilder" />
    /// <exception cref="InvalidOperationException">At least one policy must be configured.</exception>
    public IAsyncPolicy<TResult> Build()
    {
        if (Policies.Count == 0)
        {
            Throw.InvalidOperationException("At least one policy must be configured.");
        }

        var policy = Policies.Count > 1 ?
            new AsyncPolicyPipeline<TResult>(Policies) :
            Policies[0];

        if (_pipelineId != null)
        {
            policy = PipelineTelemetry.Create(_pipelineId, policy, _metering, _logger, _timeProvider).WithPolicyKey(_pipelineId.PolicyPipelineKey);
        }

        return policy;
    }
}
