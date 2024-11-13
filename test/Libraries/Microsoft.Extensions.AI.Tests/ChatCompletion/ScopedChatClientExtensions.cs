// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public static class ScopedChatClientExtensions
{
    public static ChatClientBuilder UseScopedMiddleware(this ChatClientBuilder builder)
        => builder.Use((services, inner)
            => new DependencyInjectionPatterns.ScopedChatClient(services, inner));
}
