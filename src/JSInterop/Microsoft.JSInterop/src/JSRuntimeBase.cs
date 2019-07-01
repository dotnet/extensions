// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop.Internal;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Abstract base class for a JavaScript runtime.
    /// </summary>
    public abstract class JSRuntimeBase : IJSRuntime
    {
        private long _nextPendingTaskId = 1; // Start at 1 because zero signals "no response needed"
        private readonly ConcurrentDictionary<long, object> _pendingTasks
            = new ConcurrentDictionary<long, object>();

        private readonly ConcurrentDictionary<long, CancellationTokenRegistration> _cancellationRegistrations =
            new ConcurrentDictionary<long, CancellationTokenRegistration>();

        internal DotNetObjectRefManager ObjectRefManager { get; } = new DotNetObjectRefManager();

        /// <summary>
        /// Gets or sets the default timeout for asynchronous JavaScript calls.
        /// </summary>
        protected TimeSpan? DefaultAsyncCallTimeout { get; set; }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <param name="cancellationToken">A cancellation token to signal the cancellation of the operation.</param>
        /// <returns>An instance of <typeparamref name="T"/> obtained by JSON-deserializing the return value.</returns>
        public Task<T> InvokeAsync<T>(string identifier, IEnumerable<object> args, CancellationToken cancellationToken = default)
        {
            var taskId = Interlocked.Increment(ref _nextPendingTaskId);
            var tcs = new TaskCompletionSource<T>(TaskContinuationOptions.RunContinuationsAsynchronously);
            if (cancellationToken != default)
            {
                _cancellationRegistrations[taskId] = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled();
                    CleanupRegistrations(taskId);
                });
            }
            _pendingTasks[taskId] = tcs;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    CleanupRegistrations(taskId);

                    return tcs.Task;
                }

                var argsJson = args?.Any() == true ?
                    JsonSerializer.Serialize(args, JsonSerializerOptionsProvider.Options) :
                    null;
                BeginInvokeJS(taskId, identifier, argsJson);

                return tcs.Task;
            }
            catch
            {
                CleanupRegistrations(taskId);
                throw;
            }
        }

        private void CleanupRegistrations(long taskId)
        {
            _pendingTasks.TryRemove(taskId, out _);
            _cancellationRegistrations.TryRemove(taskId, out var registration);

            // dispose will noop when registration == default
            registration.Dispose();
        }

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="T"/> obtained by JSON-deserializing the return value.</returns>
        public Task<T> InvokeAsync<T>(string identifier, params object[] args)
        {
            if (!DefaultAsyncCallTimeout.HasValue)
            {
                return InvokeAsync<T>(identifier, args, default);
            }
            else
            {
                return InvokeWithDefaultCancellation<T>(identifier, args);
            }
        }

        private async Task<T> InvokeWithDefaultCancellation<T>(string identifier, IEnumerable<object> args)
        {
            using (var cts = new CancellationTokenSource(DefaultAsyncCallTimeout.Value))
            {
                // We need to await here due to the using
                return await InvokeAsync<T>(identifier, args, cts.Token);
            }
        }

        /// <summary>
        /// Begins an asynchronous function invocation.
        /// </summary>
        /// <param name="asyncHandle">The identifier for the function invocation, or zero if no async callback is required.</param>
        /// <param name="identifier">The identifier for the function to invoke.</param>
        /// <param name="argsJson">A JSON representation of the arguments.</param>
        protected abstract void BeginInvokeJS(long asyncHandle, string identifier, string argsJson);

        internal void EndInvokeDotNet(string callId, bool success, object resultOrException)
        {
            // For failures, the common case is to call EndInvokeDotNet with the Exception object.
            // For these we'll serialize as something that's useful to receive on the JS side.
            // If the value is not an Exception, we'll just rely on it being directly JSON-serializable.
            if (!success && resultOrException is Exception)
            {
                resultOrException = resultOrException.ToString();
            }
            else if (!success && resultOrException is ExceptionDispatchInfo edi)
            {
                resultOrException = edi.SourceException.ToString();
            }

            // We pass 0 as the async handle because we don't want the JS-side code to
            // send back any notification (we're just providing a result for an existing async call)
            var args = JsonSerializer.Serialize(new[] { callId, success, resultOrException }, JsonSerializerOptionsProvider.Options);
            BeginInvokeJS(0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", args);
        }

        internal void EndInvokeJS(long asyncHandle, bool succeeded, JSAsyncCallResult asyncCallResult)
        {
            using (asyncCallResult?.JsonDocument)
            {
                if (!_pendingTasks.TryRemove(asyncHandle, out var tcs))
                {
                    // We should simply return if we can't find an id for the invocation.
                    // This likely means that the method that initiated the call defined a timeout and stopped waiting.
                    return;
                }

                CleanupRegistrations(asyncHandle);

                if (succeeded)
                {
                    var resultType = TaskGenericsUtil.GetTaskCompletionSourceResultType(tcs);
                    try
                    {
                        var result = asyncCallResult != null ?
                            JsonSerializer.Deserialize(asyncCallResult.JsonElement.GetRawText(), resultType, JsonSerializerOptionsProvider.Options) :
                            null;
                        TaskGenericsUtil.SetTaskCompletionSourceResult(tcs, result);
                    }
                    catch (Exception exception)
                    {
                        var message = $"An exception occurred executing JS interop: {exception.Message}. See InnerException for more details.";
                        TaskGenericsUtil.SetTaskCompletionSourceException(tcs, new JSException(message, exception));
                    }
                }
                else
                {
                    var exceptionText = asyncCallResult?.JsonElement.ToString() ?? string.Empty;
                    TaskGenericsUtil.SetTaskCompletionSourceException(tcs, new JSException(exceptionText));
                }
            }
        }
    }
}
