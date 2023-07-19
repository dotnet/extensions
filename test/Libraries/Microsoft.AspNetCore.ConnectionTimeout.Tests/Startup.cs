// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Connections.Test;

[SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Used through reflection")]
public class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                var connectionFeature = context.Features.Get<IHttpConnectionFeature>()!;
                context.Response.Headers.Append("ConnectionId", connectionFeature!.ConnectionId);
                await context.Response.WriteAsync("Hello World!");
            });
        });
    }
}
