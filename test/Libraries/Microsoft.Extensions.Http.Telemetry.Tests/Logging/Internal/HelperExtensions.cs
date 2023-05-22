// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

internal static class HelperExtensions
{
    public static IServiceCollection BlockRemoteCall(this IServiceCollection services)
    {
        return services
            .AddTransient<NoRemoteCallHandler>()
            .ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
                    builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<NoRemoteCallHandler>());
                });
            });
    }
}
