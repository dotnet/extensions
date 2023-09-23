// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions to attach the HTTP logging middleware.
/// </summary>
public static class HttpLoggingApplicationBuilderExtensions
{
    /// <summary>
    /// Registers incoming HTTP request logging middleware into <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Request logging middleware should be placed after <see cref="EndpointRoutingApplicationBuilderExtensions.UseRouting"/> call.
    /// </remarks>
    /// <param name="builder">An application's request pipeline builder.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseHttpLoggingMiddleware(this IApplicationBuilder builder)
        => Throw.IfNull(builder)
        .UseMiddleware<HttpLoggingMiddleware>([]);
}
