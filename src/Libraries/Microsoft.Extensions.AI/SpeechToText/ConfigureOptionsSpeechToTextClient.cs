﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that configures a <see cref="SpeechToTextOptions"/> instance used by the remainder of the pipeline.</summary>
public sealed class ConfigureOptionsSpeechToTextClient : DelegatingSpeechToTextClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<SpeechToTextOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsSpeechToTextClient"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="SpeechToTextOptions"/> instance. It is passed a clone of the caller-supplied <see cref="SpeechToTextOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="SpeechToTextOptions"/> if
    /// the caller didn't supply a <see cref="SpeechToTextOptions"/> instance, or a clone (via <see cref="SpeechToTextOptions.Clone"/> of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsSpeechToTextClient(ISpeechToTextClient innerClient, Action<SpeechToTextOptions> configure)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override Task<SpeechToTextResponse> GetResponseAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        return base.GetResponseAsync(speechContents, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingResponseAsync(
        IList<IAsyncEnumerable<DataContent>> speechContents, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        return base.GetStreamingResponseAsync(speechContents, Configure(options), cancellationToken);
    }

    /// <summary>Creates and configures the <see cref="SpeechToTextOptions"/> to pass along to the inner client.</summary>
    private SpeechToTextOptions Configure(SpeechToTextOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}
