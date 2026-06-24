// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public static class SingletonTextToSpeechClientExtensions
{
    public static TextToSpeechClientBuilder UseSingletonMiddleware(this TextToSpeechClientBuilder builder)
        => builder.Use((inner, services)
            => new TextToSpeechClientDependencyInjectionPatterns.SingletonMiddleware(inner, services));
}
