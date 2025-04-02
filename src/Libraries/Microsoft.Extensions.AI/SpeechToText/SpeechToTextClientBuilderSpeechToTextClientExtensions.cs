// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="ISpeechToTextClient"/> in the context of <see cref="SpeechToTextClientBuilder"/>.</summary>
[Experimental("MEAI001")]
public static class SpeechToTextClientBuilderSpeechToTextClientExtensions
{
    /// <summary>Creates a new <see cref="SpeechToTextClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="SpeechToTextClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="SpeechToTextClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    public static SpeechToTextClientBuilder AsBuilder(this ISpeechToTextClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new SpeechToTextClientBuilder(innerClient);
    }
}
