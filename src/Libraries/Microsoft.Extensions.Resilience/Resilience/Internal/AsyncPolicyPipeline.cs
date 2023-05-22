// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

#pragma warning disable S109 // Magic numbers should not be used

/// <summary>
/// The Async policy running the sequence of other async policies as pipelines.
/// </summary>
internal sealed partial class AsyncPolicyPipeline : AsyncPolicy
{
    private delegate Task<TResult> ExecuteDelegate<TResult>(PollyExecuteAsyncArguments<TResult> args);

    /// <summary>
    /// The maximum number of policies for which we support a zero-allocation implementation.
    /// Above this number we use implementation allocating callback actions as array.
    /// </summary>
    /// <remarks>This value was selected based on the maximum number of layers that are used by us in the standard pipeline.</remarks>
    private const int MaximumPoliciesOptimized = 5;

    private const string ArgsKey = "AsyncPolicyPipeline.PollyExecuteAsyncArguments";

    private readonly ConcurrentDictionary<Type, object> _executeDelegates = new();
    private readonly IAsyncPolicy[] _frozenPolicies;
    private readonly Policies _policies;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncPolicyPipeline"/> class.
    /// </summary>
    /// <param name="policies">The sequence of policies to run.</param>
    public AsyncPolicyPipeline(IReadOnlyList<IAsyncPolicy> policies)
    {
        if (policies.Count == 0)
        {
            Throw.ArgumentException(nameof(policies), "Argument is an empty collection");
        }

        _frozenPolicies = policies.ToArray();
        _policies = new Policies(policies);
    }

    protected override async Task<TResult> ImplementationAsync<TResult>(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        var args = ArgPool<TResult>.Instance.Get();
        args.Action = action;
        args.Context = context;
        args.CancellationToken = cancellationToken;
        args.Policies = _policies;
        args.ContinueOnCapturedContext = continueOnCapturedContext;

        context[ArgsKey] = args;

        var executeDelegate = GetExecuteDelegate<TResult>();

        try
        {
            return await executeDelegate(args).ConfigureAwait(false);
        }
        finally
        {
            ArgPool<TResult>.Instance.Return(args);
        }
    }

    private static ExecuteDelegate<T> CreateExecuteDelegate1<T>()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(args.Action, args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate<T> CreateExecuteDelegate2<T>()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs<T>(context);
                return args.Policies!.Policy1!.ExecuteAsync(args.Action, context, cancellationToken, args.ContinueOnCapturedContext);
            },
            args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate<T> CreateExecuteDelegate3<T>()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs<T>(context);
                return args.Policies!.Policy1!.ExecuteAsync(
                    static (context, cancellationToken) =>
                    {
                        var args = GetArgs<T>(context);
                        return args.Policies!.Policy2!.ExecuteAsync(args.Action, context, cancellationToken, args.ContinueOnCapturedContext);
                    },
                    context, cancellationToken, args.ContinueOnCapturedContext);
            },
            args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate<T> CreateExecuteDelegate4<T>()
    {
        return static args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs<T>(context);
                return args.Policies!.Policy1!.ExecuteAsync(
                    static (context, cancellationToken) =>
                    {
                        var args = GetArgs<T>(context);
                        return args.Policies!.Policy2!.ExecuteAsync(
                            static (context, cancellationToken) =>
                            {
                                var args = GetArgs<T>(context);
                                return args.Policies!.Policy3!.ExecuteAsync(args.Action, context, cancellationToken, args.ContinueOnCapturedContext);
                            },
                            context, cancellationToken, args.ContinueOnCapturedContext);
                    },
                    context, cancellationToken, args.ContinueOnCapturedContext);
            },
            args.Context, args.CancellationToken, args.ContinueOnCapturedContext);
    }

    private static ExecuteDelegate<T> CreateExecuteDelegate5<T>()
    {
        return args => args.Policies!.Policy0!.ExecuteAsync(
            static (context, cancellationToken) =>
            {
                var args = GetArgs<T>(context);
                return args.Policies!.Policy1!.ExecuteAsync(
                    static (context, cancellationToken) =>
                    {
                        var args = GetArgs<T>(context);
                        return args.Policies!.Policy2!.ExecuteAsync(
                            static (context, cancellationToken) =>
                            {
                                var args = GetArgs<T>(context);
                                return args.Policies!.Policy3!.ExecuteAsync(
                                    static (context, cancellationToken) =>
                                    {
                                        var args = GetArgs<T>(context);
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

    private static ExecuteDelegate<T> CreateExecuteDelegateFromManyPolicies<T>(IAsyncPolicy[] policies)
    {
        policies = policies.Reverse().ToArray();

        var actions = new ExecuteDelegate<T>[policies.Length];
        actions[0] = args =>
            policies[0].ExecuteAsync(
                args.Action,
                args.Context,
                args.CancellationToken,
                args.ContinueOnCapturedContext);

        for (int i = 1; i < policies.Length; i++)
        {
            var nextPolicy = policies[i];
            var nextAction = actions[i - 1];
            actions[i] = (args) =>
                nextPolicy.ExecuteAsync(
                       (context, _) => nextAction(GetArgs<T>(context)),
                       args.Context,
                       args.CancellationToken,
                       args.ContinueOnCapturedContext);
        }

        return actions[actions.Length - 1];
    }

    private static ExecuteDelegate<T> CreateExecuteDelegate<T>(IAsyncPolicy[] policies)
    {
        return policies.Length switch
        {
            1 => CreateExecuteDelegate1<T>(),
            2 => CreateExecuteDelegate2<T>(),
            3 => CreateExecuteDelegate3<T>(),
            4 => CreateExecuteDelegate4<T>(),
            MaximumPoliciesOptimized => CreateExecuteDelegate5<T>(),
            _ => CreateExecuteDelegateFromManyPolicies<T>(policies)
        };
    }

    private static PollyExecuteAsyncArguments<T> GetArgs<T>(Context ctxt) => (PollyExecuteAsyncArguments<T>)ctxt[ArgsKey];

    private ExecuteDelegate<T> GetExecuteDelegate<T>()
    {
        return (ExecuteDelegate<T>)_executeDelegates.GetOrAdd(typeof(T), key => CreateExecuteDelegate<T>(_frozenPolicies));
    }
}
#pragma warning restore S109 // Magic numbers should not be used
