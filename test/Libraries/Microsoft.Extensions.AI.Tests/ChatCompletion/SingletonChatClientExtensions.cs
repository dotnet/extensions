// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public static class SingletonChatClientExtensions
{
    public static ChatClientBuilder UseSingletonMiddleware(this ChatClientBuilder builder)
        => builder.Use((services, inner)
            => new DependencyInjectionPatterns.SingletonMiddleware(services, inner));
}
