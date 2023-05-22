// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.EnumStrings;
using Polly;

[assembly: EnumStrings(typeof(HttpStatusCode))]

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Static methods to extract the failure reason of a call sent using polices.
/// </summary>
internal static class FailureReasonResolver
{
    private const string Undefined = "Undefined";
    private static readonly ConcurrentDictionary<HttpStatusCode, string> _statusCodeCache = new();

    /// <summary>
    /// Gets the failure reason.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>Reason string defining the failure's reason.</returns>
    public static string GetFailureReason<TResult>(DelegateResult<TResult> result)
    {
        if (Equals(result.Result, default(TResult)) && Equals(result.Exception, null))
        {
            return Undefined;
        }

        var errMessage = GetFailureFromException(result.Exception);

        if (errMessage == Undefined)
        {
            var msg = result.Result as HttpResponseMessage;
            return msg == null ? Undefined : $"Status code: {GetOrAddStatusCodeString(msg.StatusCode, () => msg.StatusCode.ToInvariantString())}";
        }

        return errMessage;
    }

    public static string GetFailureFromException(Exception e)
    {
        var errMessage = e?.Message;
        if (!string.IsNullOrWhiteSpace(errMessage))
        {
            return $"Error: {errMessage}";
        }

        return Undefined;
    }

    private static string GetOrAddStatusCodeString(HttpStatusCode key, Func<string> createItem)
    {
        _ = _statusCodeCache.TryAdd(key, createItem());
        return _statusCodeCache[key];
    }
}
