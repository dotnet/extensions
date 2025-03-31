// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that configures a <see cref="SpeechToTextOptions"/> instance used by the remainder of the pipeline.</summary>
[Experimental("MEAI001")]
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
    public override async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.GetTextAsync(audioSpeechStream, Configure(options), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingTextAsync(audioSpeechStream, Configure(options), cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <summary>Creates and configures the <see cref="SpeechToTextOptions"/> to pass along to the inner client.</summary>
    private SpeechToTextOptions Configure(SpeechToTextOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}
