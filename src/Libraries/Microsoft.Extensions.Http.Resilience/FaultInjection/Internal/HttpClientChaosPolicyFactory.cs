// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.EnumStrings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Extensions.Telemetry.Metrics;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;

[assembly: EnumStrings(typeof(HttpStatusCode))]

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

/// <summary>
/// Default implementation of <see cref="IHttpClientChaosPolicyFactory"/>.
/// </summary>
internal sealed class HttpClientChaosPolicyFactory : IHttpClientChaosPolicyFactory
{
    private const string FaultTypeHttpStatus = "HttpStatus";

    private readonly Task<bool> _enabled = Task.FromResult(true);
    private readonly Task<bool> _notEnabled = Task.FromResult(false);
    private readonly Task<double> _noInjectionRate = Task.FromResult<double>(0);

    private readonly ILogger<IHttpClientChaosPolicyFactory> _logger;
    private readonly HttpClientFaultInjectionMetricCounter _counter;
    private readonly IFaultInjectionOptionsProvider _optionsProvider;
    private readonly IHttpContentOptionsRegistry _httpContentOptionsRegistry;
    private readonly Func<Context, CancellationToken, Task<HttpResponseMessage>> _getHttpResponseMessageAsync;
    private readonly Func<Context, CancellationToken, Task<bool>> _getEnabledAsync;
    private readonly Func<Context, CancellationToken, Task<double>> _getInjectionRateAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientChaosPolicyFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="meter">The meter.</param>
    /// <param name="optionsProvider">The provider of <see cref="FaultInjectionOptions"/>.</param>
    /// <param name="httpContentOptionsRegistry">The registry that contains registered http content options.</param>
    public HttpClientChaosPolicyFactory(
        ILogger<IHttpClientChaosPolicyFactory> logger, Meter<IHttpClientChaosPolicyFactory> meter,
        IFaultInjectionOptionsProvider optionsProvider, IHttpContentOptionsRegistry httpContentOptionsRegistry)
    {
        _logger = logger;
        _counter = Metric.CreateHttpClientFaultInjectionMetricCounter(meter);
        _optionsProvider = optionsProvider;
        _httpContentOptionsRegistry = httpContentOptionsRegistry;
        _getHttpResponseMessageAsync = GetHttpResponseMessageAsync;
        _getEnabledAsync = GetEnabledAsync;
        _getInjectionRateAsync = GetInjectionRateAsync;
    }

    /// <inheritdoc/>
    public AsyncInjectOutcomePolicy<HttpResponseMessage> CreateHttpResponsePolicy()
    {
        return MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(with =>
            with.Result(_getHttpResponseMessageAsync)
            .InjectionRate(_getInjectionRateAsync)
            .EnabledWhen(_getEnabledAsync));
    }

    /// <summary>
    /// Fault provider task for <see cref="CreateHttpResponsePolicy()"/>.
    /// </summary>
    /// <remarks>
    /// This task only gets executed when HttpResponseInjectionPolicyOptions is defined at the options group and is enabled,
    /// as defined in <see cref="GetEnabledAsync"/>.
    /// See how faults are injected at <see href="https://github.com/Polly-Contrib/Simmy/blob/master/src/Polly.Contrib.Simmy/AsyncMonkeyEngine.cs"/>.
    /// </remarks>
    internal Task<HttpResponseMessage> GetHttpResponseMessageAsync(Context context, CancellationToken ct)
    {
        var groupName = context.GetFaultInjectionGroupName()!;
        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);

        var statusCode = optionsGroup!.HttpResponseInjectionPolicyOptions!.StatusCode;
        var httpContentKey = optionsGroup!.HttpResponseInjectionPolicyOptions.HttpContentKey;
        var httpContent = httpContentKey == null ? null : _httpContentOptionsRegistry.GetHttpContent(httpContentKey);

        var response = new HttpResponseMessage(statusCode);
        if (httpContent != null)
        {
            response.Content = httpContent;
        }

        response.RequestMessage = context.GetCallingRequestMessage();

        FaultInjectionTelemetryHandler.LogAndMeter(
            _logger, _counter, groupName,
            FaultTypeHttpStatus, statusCode.ToInvariantString(), httpContentKey);

        return Task.FromResult(response);
    }

    /// <summary>
    /// Task for checking if fault-injection is enabled from the <see cref="Context"/>'s associated chaos policy options.
    /// </summary>
    internal Task<bool> GetEnabledAsync(Context context, CancellationToken ct)
    {
        var groupName = context.GetFaultInjectionGroupName();
        if (groupName == null)
        {
            return _notEnabled;
        }

        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);
        if (optionsGroup?.HttpResponseInjectionPolicyOptions?.Enabled ?? false)
        {
            return _enabled;
        }

        return _notEnabled;
    }

    /// <summary>
    /// Task for checking the injection rate from the <see cref="Context"/>'s associated chaos policy options.
    /// </summary>
    internal Task<double> GetInjectionRateAsync(Context context, CancellationToken ct)
    {
        var groupName = context.GetFaultInjectionGroupName();
        if (groupName == null)
        {
            return _noInjectionRate;
        }

        _ = _optionsProvider.TryGetChaosPolicyOptionsGroup(groupName, out var optionsGroup);
        var opt = optionsGroup?.HttpResponseInjectionPolicyOptions;
        if (opt == null)
        {
            return _noInjectionRate;
        }

        return Task.FromResult(opt.FaultInjectionRate);
    }
}
