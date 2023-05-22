// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

#pragma warning disable S109 // Magic numbers should not be used
/// <summary>
/// The Async policy running the sequence of other async policies as pipelines.
/// </summary>
/// <typeparam name="TResult">The type of the result handled by the policy.</typeparam>
internal sealed partial class AsyncPolicyPipeline<TResult> : AsyncPolicy<TResult>
{
    private delegate Task<TResult> ExecuteDelegate(PollyExecuteAsyncArguments args);

    /// <summary>
    /// The maximum number of policies for which we support a zero-allocation implementation.
    /// Above this number we use implementation allocating callback actions as array.
    /// </summary>
    /// <remarks>This value was selected based on the maximum number of layers that are used by us in the standard pipeline.</remarks>
    private const int MaximumPoliciesOptimized = 5;

    private const string ArgsKey = "AsyncPolicyPipeline.PollyExecuteAsyncArguments";

    private static readonly ObjectPool<PollyExecuteAsyncArguments> _argPool = PoolFactory.CreatePool<PollyExecuteAsyncArguments>();

    private readonly ExecuteDelegate _executeDelegate;
    private readonly Policies _policies;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncPolicyPipeline{TResult}"/> class.
    /// </summary>
    /// <param name="policies">The sequence of policies to run.</param>
    public AsyncPolicyPipeline(IReadOnlyList<IAsyncPolicy<TResult>> policies)
    {
        if (policies.Count == 0)
        {
            Throw.ArgumentException(nameof(policies), "Argument is an empty collection");
        }

        _executeDelegate = policies.Count switch
        {
            1 => CreateExecuteDelegate1(),
            2 => CreateExecuteDelegate2(),
            3 => CreateExecuteDelegate3(),
            4 => CreateExecuteDelegate4(),
            MaximumPoliciesOptimized => CreateExecuteDelegate5(),
            _ => CreateExecuteDelegate(policies)
        };
        _policies = new Policies(policies);
    }

    protected override async Task<TResult> ImplementationAsync(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        var args = _argPool.Get();
        args.Action = action;
        args.Context = context;
        args.CancellationToken = cancellationToken;
        args.Policies = _policies;
        args.ContinueOnCapturedContext = continueOnCapturedContext;

        context[ArgsKey] = args;

        try
        {
            return await _executeDelegate(args).ConfigureAwait(false);
        }
        finally
        {
            _argPool.Return(args);
        }
    }

    private static ExecuteDelegate CreateExecuteDelegate1()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(args.Action, args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate CreateExecuteDelegate2()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs(context);
                return args.Policies!.Policy1!.ExecuteAsync(args.Action, context, cancellationToken, args.ContinueOnCapturedContext);
            },
            args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate CreateExecuteDelegate3()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs(context);
                return args.Policies!.Policy1!.ExecuteAsync(
                    static (context, cancellationToken) =>
                    {
                        var args = GetArgs(context);
                        return args.Policies!.Policy2!.ExecuteAsync(args.Action, context, cancellationToken, args.ContinueOnCapturedContext);
                    },
                    context, cancellationToken, args.ContinueOnCapturedContext);
            },
            args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate CreateExecuteDelegate4()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs(context);
                return args.Policies!.Policy1!.ExecuteAsync(
                    static (context, cancellationToken) =>
                    {
                        var args = GetArgs(context);
                        return args.Policies!.Policy2!.ExecuteAsync(
                            static (context, cancellationToken) =>
                            {
                                var args = GetArgs(context);
                                return args.Policies!.Policy3!.ExecuteAsync(args.Action, context, cancellationToken, args.ContinueOnCapturedContext);
                            },
                            context, cancellationToken, args.ContinueOnCapturedContext);
                    },
                    context, cancellationToken, args.ContinueOnCapturedContext);
            },
            args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate CreateExecuteDelegate5()
    {
        return args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs(context);
                return args.Policies!.Policy1!.ExecuteAsync(
                    static (context, cancellationToken) =>
                    {
                        var args = GetArgs(context);
                        return args.Policies!.Policy2!.ExecuteAsync(
                            static (context, cancellationToken) =>
                            {
                                var args = GetArgs(context);
                                return args.Policies!.Policy3!.ExecuteAsync(
                                    static (context, cancellationToken) =>
                                    {
                                        var args = GetArgs(context);
                                        return args.Policies!.Policy4!.ExecuteAsync(args.Action, context, cancellationToken, args.ContinueOnCapturedContext);
                                    },
                                    context, cancellationToken, args.ContinueOnCapturedContext);
                            },
                            context, cancellationToken, args.ContinueOnCapturedContext);
                    },
                    context, cancellationToken, args.ContinueOnCapturedContext);
            },
            args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate CreateExecuteDelegate(IReadOnlyList<IAsyncPolicy<TResult>> policies)
    {
        policies = policies.Reverse().ToArray();

        var actions = new ExecuteDelegate[policies.Count];
        actions[0] = args =>
            policies[0].ExecuteAsync(
                args.Action,
                args.Context,
                args.CancellationToken,
                args.ContinueOnCapturedContext);

        for (int i = 1; i < policies.Count; i++)
        {
            var nextPolicy = policies[i];
            var nextAction = actions[i - 1];
            actions[i] = (args) =>
                nextPolicy.ExecuteAsync(
                       (context, _) => nextAction(GetArgs(context)),
                       args.Context,
                       args.CancellationToken,
                       args.ContinueOnCapturedContext);
        }

        return actions[actions.Length - 1];
    }

    private static PollyExecuteAsyncArguments GetArgs(Context ctxt) => (PollyExecuteAsyncArguments)ctxt[ArgsKey];
}
#pragma warning restore S109 // Magic numbers should not be used
