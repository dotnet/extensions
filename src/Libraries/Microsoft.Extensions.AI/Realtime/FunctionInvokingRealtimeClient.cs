// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating realtime client that invokes functions defined on <see cref="RealtimeClientCreateResponseMessage"/>.
/// Include this in a realtime client pipeline to resolve function calls automatically.
/// </summary>
/// <remarks>
/// <para>
/// When sessions created by this client receive a <see cref="FunctionCallContent"/> in a realtime server message from the inner
/// <see cref="IRealtimeClientSession"/>, they respond by invoking the corresponding <see cref="AIFunction"/> defined
/// in <see cref="RealtimeClientCreateResponseMessage.Tools"/> (or in <see cref="AdditionalTools"/>), producing a <see cref="FunctionResultContent"/>
/// that is sent back to the inner session. This loop is repeated until there are no more function calls to make, or until
/// another stop condition is met, such as hitting <see cref="MaximumIterationsPerRequest"/>.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class FunctionInvokingRealtimeClient : DelegatingRealtimeClient
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IServiceProvider? _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvokingRealtimeClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner <see cref="IRealtimeClient"/>.</param>
    /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> to use for logging information about function invocation.</param>
    /// <param name="functionInvocationServices">An optional <see cref="IServiceProvider"/> to use for resolving services required by the <see cref="AIFunction"/> instances being invoked.</param>
    public FunctionInvokingRealtimeClient(IRealtimeClient innerClient, ILoggerFactory? loggerFactory = null, IServiceProvider? functionInvocationServices = null)
        : base(innerClient)
    {
        _loggerFactory = loggerFactory;
        _services = functionInvocationServices;
    }

    /// <summary>
    /// Gets the <see cref="FunctionInvocationContext"/> for the current function invocation.
    /// </summary>
    /// <remarks>
    /// This value flows across async calls.
    /// </remarks>
    public static FunctionInvocationContext? CurrentContext => FunctionInvokingRealtimeClientSession.CurrentContext;

    /// <summary>
    /// Gets or sets a value indicating whether detailed exception information should be included
    /// in the response when calling the underlying <see cref="IRealtimeClientSession"/>.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the full exception message is added to the response.
    /// <see langword="false"/> if a generic error message is included in the response.
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool IncludeDetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow concurrent invocation of functions.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if multiple function calls can execute in parallel.
    /// <see langword="false"/> if function calls are processed serially.
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool AllowConcurrentInvocation { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of iterations per request.
    /// </summary>
    /// <value>
    /// The maximum number of iterations per request.
    /// The default value is 40.
    /// </value>
    public int MaximumIterationsPerRequest
    {
        get;
        set
        {
            if (value < 1)
            {
                Throw.ArgumentOutOfRangeException(nameof(value));
            }

            field = value;
        }
    } = 40;

    /// <summary>
    /// Gets or sets the maximum number of consecutive iterations that are allowed to fail with an error.
    /// </summary>
    /// <value>
    /// The maximum number of consecutive iterations that are allowed to fail with an error.
    /// The default value is 3.
    /// </value>
    public int MaximumConsecutiveErrorsPerRequest
    {
        get;
        set => field = Throw.IfLessThan(value, 0);
    } = 3;

    /// <summary>Gets or sets a collection of additional tools the session is able to invoke.</summary>
    public IList<AITool>? AdditionalTools { get; set; }

    /// <summary>Gets or sets a value indicating whether a request to call an unknown function should terminate the function calling loop.</summary>
    /// <value>
    /// <see langword="true"/> to terminate the function calling loop and return the response if a request to call a tool
    /// that isn't available is received; <see langword="false"/> to create and send a
    /// function result message stating that the tool couldn't be found. The default is <see langword="false"/>.
    /// </value>
    public bool TerminateOnUnknownCalls { get; set; }

    /// <summary>Gets or sets a delegate used to invoke <see cref="AIFunction"/> instances.</summary>
    public Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>? FunctionInvoker { get; set; }

    /// <inheritdoc />
    public override async Task<IRealtimeClientSession> CreateSessionAsync(
        RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var innerSession = await base.CreateSessionAsync(options, cancellationToken).ConfigureAwait(false);
        return new FunctionInvokingRealtimeClientSession(innerSession, this, _loggerFactory, _services);
    }
}
