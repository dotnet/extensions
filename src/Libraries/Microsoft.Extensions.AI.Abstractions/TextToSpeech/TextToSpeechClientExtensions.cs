// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Extensions for <see cref="ITextToSpeechClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public static class TextToSpeechClientExtensions
{
    /// <summary>Asks the <see cref="ITextToSpeechClient"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="ITextToSpeechClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this ITextToSpeechClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);

        return (TService?)client.GetService(typeof(TService), serviceKey);
    }
}
