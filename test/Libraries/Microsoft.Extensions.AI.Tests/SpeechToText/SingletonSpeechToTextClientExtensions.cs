// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public static class SingletonSpeechToTextClientExtensions
{
    public static SpeechToTextClientBuilder UseSingletonMiddleware(this SpeechToTextClientBuilder builder)
        => builder.Use((inner, services)
            => new SpeechToTextClientDependencyInjectionPatterns.SingletonMiddleware(inner, services));
}
