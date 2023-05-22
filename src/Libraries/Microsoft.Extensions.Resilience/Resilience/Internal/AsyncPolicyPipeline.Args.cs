// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed partial class AsyncPolicyPipeline
{
    private static class ArgPool<T>
    {
        public static readonly ObjectPool<PollyExecuteAsyncArguments<T>> Instance = PoolFactory.CreatePool<PollyExecuteAsyncArguments<T>>();
    }

    /// <summary>
    /// Structure with the arguments of the execute methods.
    /// </summary>
    private sealed class PollyExecuteAsyncArguments<TResult>
    {
        public Func<Context, CancellationToken, Task<TResult>>? Action { get; set; }

        public Context? Context { get; set; }

        public bool ContinueOnCapturedContext { get; set; }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public Policies? Policies { get; set; }
    }

    private sealed class Policies
    {
        public Policies(IReadOnlyList<IAsyncPolicy> policies)
        {
#pragma warning disable S109 // Magic numbers should not be used
            Policy0 = GetPolicy(policies, 0);
            Policy1 = GetPolicy(policies, 1);
            Policy2 = GetPolicy(policies, 2);
            Policy3 = GetPolicy(policies, 3);
            Policy4 = GetPolicy(policies, 4);
#pragma warning restore S109 // Magic numbers should not be used
        }

        public IAsyncPolicy? Policy0 { get; }

        public IAsyncPolicy? Policy1 { get; }

        public IAsyncPolicy? Policy2 { get; }

        public IAsyncPolicy? Policy3 { get; }

        public IAsyncPolicy? Policy4 { get; }

        private static IAsyncPolicy? GetPolicy(IReadOnlyList<IAsyncPolicy> policies, int index)
        {
            if (index >= policies.Count)
            {
                return null;
            }

            return policies[index];
        }
    }
}
