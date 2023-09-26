// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Extensions.Http.Logging.Test.Internal;

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

    public static IOptionsMonitor<LoggingOptions> ToOptionsMonitor(this LoggingOptions options, string? key = null)
    {
        var snapshotMock = new Mock<IOptionsMonitor<LoggingOptions>>();

        if (key is not null)
        {
            snapshotMock
                .Setup(monitor => monitor.Get(key))
                .Returns(options);
        }
        else
        {
            snapshotMock
                .SetupGet(monitor => monitor.CurrentValue)
                .Returns(options);
        }

        return snapshotMock.Object;
    }
}
