// Assembly 'Microsoft.AspNetCore.ConnectionTimeout'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Connections;

public static class ConnectionTimeoutExtensions
{
    public static ListenOptions UseConnectionTimeout(this ListenOptions listenOptions);
    public static IServiceCollection AddConnectionTimeout(this IServiceCollection services, Action<ConnectionTimeoutOptions> configure);
    public static IServiceCollection AddConnectionTimeout(this IServiceCollection services, IConfigurationSection section);
}
