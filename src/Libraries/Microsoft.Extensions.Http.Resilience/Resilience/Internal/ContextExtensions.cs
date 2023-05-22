// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Various extensions for <see cref="Context"/>.
/// </summary>
internal static class ContextExtensions
{
    private const string RequestMessageKey = "Resilience.ContextExtensions.Request";

    private const string MessageInvokerKey = "Resilience.ContextExtensions.MessageInvoker";

    /// <summary>
    /// Sets the request metadata to the context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="request">The request.</param>
    public static void SetRequestMetadata(this Context context, HttpRequestMessage request)
    {
        _ = Throw.IfNull(request);
        _ = Throw.IfNull(context);

        if (!context.ContainsKey(TelemetryConstants.RequestMetadataKey) && request.GetRequestMetadata() is RequestMetadata requestMetadata)
        {
            context[TelemetryConstants.RequestMetadataKey] = requestMetadata;
        }
    }

    /// <summary>
    /// Gets the <see cref="HttpMessageInvoker"/> assigned to the context.
    /// </summary>
    /// <returns>A <see cref="HttpMessageInvoker"/>.</returns>
    public static Func<Context, HttpMessageInvoker?> CreateMessageInvokerProvider(string pipelineName)
    {
        _ = Throw.IfNullOrEmpty(pipelineName);

        var key = $"{MessageInvokerKey}-{pipelineName}";

        return (context) =>
        {
            if (context.TryGetValue(key, out var val))
            {
                return ((Lazy<HttpMessageInvoker>)val).Value;
            }

            return null;
        };
    }

    /// <summary>
    /// Gets the <see cref="HttpRequestMessage"/> assigned to the context.
    /// </summary>
    /// <returns>A <see cref="HttpRequestMessage"/>.</returns>
    public static Func<Context, HttpRequestMessage?> CreateRequestMessageProvider(string pipelineName)
    {
        _ = Throw.IfNullOrEmpty(pipelineName);

        var key = $"{RequestMessageKey}-{pipelineName}";

        return (context) =>
        {
            if (context.TryGetValue(key, out var val))
            {
                return (HttpRequestMessage)val;
            }

            return null;
        };
    }

    internal static Action<Context, Lazy<HttpMessageInvoker>> CreateMessageInvokerSetter(string pipelineName)
    {
        var key = $"{MessageInvokerKey}-{pipelineName}";

        return (context, invoker) => context[key] = invoker;
    }

    internal static Action<Context, HttpRequestMessage> CreateRequestMessageSetter(string pipelineName)
    {
        var key = $"{RequestMessageKey}-{pipelineName}";

        return (context, invoker) => context[key] = invoker;
    }
}
