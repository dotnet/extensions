// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Controls exception summarization.
/// </summary>
public static class ExceptionSummarizationBuilderExtensions
{
    /// <summary>
    /// Registers a summary provider that handles <see cref="OperationCanceledException"/>, <see cref="WebException"/>, and <see cref="SocketException"/> .
    /// </summary>
    /// <param name="builder">The builder to attach the provider to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IExceptionSummarizationBuilder AddHttpProvider(this IExceptionSummarizationBuilder builder)
        => Throw.IfNull(builder).AddProvider<HttpExceptionSummaryProvider>();
}
