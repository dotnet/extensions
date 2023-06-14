// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.HttpClient.SocketHandling;

/// <summary>Provides a state bag of settings for configuring <see cref="SocketsHttpHandler"/>.</summary>
public class SocketsHttpHandlerOptions
{
    private const int MaxConnectionsPerServerUpperLimit = 100_000;
    private static readonly TimeSpan _defaultConnectTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _defaultConnectionLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _defaultConnectionIdleTimeout = TimeSpan.FromSeconds(110);
#if NET5_0_OR_GREATER
    private static readonly TimeSpan _defaultKeepAlivePingDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _defaultKeepAlivePingTimeout = TimeSpan.FromSeconds(30);
#endif

    /// <summary>
    /// Gets or sets a value indicating whether to automatically follow redirection responses.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    public bool AllowAutoRedirect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use cookies when sending requests.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    public bool UseCookies { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent connections (per server endpoint) allowed when making requests.
    /// </summary>
    /// <value>
    /// The default value is `100000`.
    /// </value>
    [Range(1, MaxConnectionsPerServerUpperLimit)]
    public int MaxConnectionsPerServer { get; set; } = MaxConnectionsPerServerUpperLimit;

    /// <summary>
    /// Gets or sets the type of decompression method used by the handler for automatic decompression of the HTTP content response.
    /// </summary>
    /// <value>
    /// The default value is `All`.
    /// </value>
    public DecompressionMethods AutomaticDecompression { get; set; } = DecompressionMethods.All;

    /// <summary>
    /// Gets or sets the length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
    /// </summary>
    /// <value>
    /// The default value is 10 seconds. 100 minutes is the max timeout value in Azure SLB.
    /// </value>
    [TimeSpan("00:00:05", "00:05:00")]
    public TimeSpan ConnectTimeout { get; set; } = _defaultConnectTimeout;

    /// <summary>
    /// Gets or sets how long a connection can be in the pool to be considered reusable.
    /// </summary>
    /// <value>
    /// The default value is 5 minutes.
    /// </value>
    [TimeSpan("00:00:01", "00:15:00")]
    public TimeSpan PooledConnectionLifetime { get; set; } = _defaultConnectionLifetime;

    /// <summary>
    /// Gets or sets how long a connection can be idle in the pool to be considered reusable.
    /// </summary>
    /// <value>
    /// The default value is 3 minutes.
    /// </value>
    [TimeSpan("00:00:01", "01:40:00")]
    public TimeSpan PooledConnectionIdleTimeout { get; set; } = _defaultConnectionIdleTimeout;

#if NET5_0_OR_GREATER
    /// <summary>
    /// Gets or sets the keep alive ping delay.
    /// </summary>
    /// <value>
    /// The default value is 1 minute.
    /// </value>
    [TimeSpan("00:00:01", "01:00:00")]
    public TimeSpan KeepAlivePingDelay { get; set; } = _defaultKeepAlivePingDelay;

    /// <summary>
    /// Gets or sets the keep alive ping timeout.
    /// </summary>
    /// <value>
    /// The default value is 30 seconds.
    /// </value>
    [TimeSpan("00:00:01", "00:05:00")]
    public TimeSpan KeepAlivePingTimeout { get; set; } = _defaultKeepAlivePingTimeout;
#endif
}
