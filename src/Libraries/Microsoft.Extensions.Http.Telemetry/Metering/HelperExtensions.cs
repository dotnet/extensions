// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

internal static class HelperExtensions
{
    internal static HttpStatusCode GetStatusCode(this Exception ex)
    {
        if (ex is TaskCanceledException)
        {
            return HttpStatusCode.GatewayTimeout;
        }
        else if (ex is HttpRequestException exception)
        {
#if NET5_0_OR_GREATER
            return exception.StatusCode ?? HttpStatusCode.ServiceUnavailable;
#else
            return HttpStatusCode.ServiceUnavailable;
#endif
        }
        else
        {
            return HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Classifies the result of the HTTP response.
    /// </summary>
    /// <param name="statusCode">An <see cref="HttpStatusCode"/> to categorize the status code of HTTP response.</param>
    /// <returns><see cref="HttpRequestResultType"/> type of HTTP response and its <see cref="HttpStatusCode"/>.</returns>
    internal static HttpRequestResultType GetResultCategory(this HttpStatusCode statusCode)
    {
        if (statusCode >= HttpStatusCode.Continue && statusCode < HttpStatusCode.BadRequest)
        {
            return HttpRequestResultType.Success;
        }
        else if (statusCode >= HttpStatusCode.BadRequest && statusCode < HttpStatusCode.InternalServerError)
        {
            return HttpRequestResultType.ExpectedFailure;
        }

        return HttpRequestResultType.Failure;
    }
}
