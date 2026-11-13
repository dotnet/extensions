// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

internal static class OpenAIStreamingHelpers
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo> _taskResultProperties = new();

#pragma warning disable EA0014 // The OpenAI streaming call receives the cancellation token when creating the task.
#pragma warning disable IL2070 // The runtime task type is Task<T> returned by an OpenAI streaming method.
#pragma warning disable IL2075 // The runtime task type is Task<T> returned by an OpenAI streaming method.
    internal static async Task<IAsyncEnumerable<T>> GetAsyncEnumerableAsync<T>(object result)
    {
        // OpenAI 2.15 changed streaming methods to return a task, while 2.14 returns the stream directly.
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            PropertyInfo resultProperty = _taskResultProperties.GetOrAdd(task.GetType(), static taskType =>
                taskType.GetProperty(nameof(Task<object>.Result)) ??
                throw new InvalidOperationException("The OpenAI streaming method returned a task without a result."));
            result = resultProperty.GetValue(task) ??
                throw new InvalidOperationException("The OpenAI streaming method returned a task without a result.");
        }

        return result as IAsyncEnumerable<T> ??
            throw new InvalidOperationException($"The OpenAI streaming result does not implement {typeof(IAsyncEnumerable<T>)}.");
    }
#pragma warning restore IL2075
#pragma warning restore IL2070
#pragma warning restore EA0014
}
