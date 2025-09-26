// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public static class SingletonImageGeneratorExtensions
{
    public static ImageGeneratorBuilder UseSingletonMiddleware(this ImageGeneratorBuilder builder)
        => builder.Use((inner, services)
            => new ImageGeneratorDependencyInjectionPatterns.SingletonMiddleware(inner, services));
}
