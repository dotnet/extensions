// Assembly 'Microsoft.Extensions.Options.Contextual'

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options.Contextual;

public static class ContextualOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddContextualOptions(this IServiceCollection services);
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions) where TOptions : class;
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions) where TOptions : class;
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;
    public static IServiceCollection PostConfigureAll<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;
    public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;
    public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, string? name, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;
    public static IServiceCollection ValidateContextualOptions<TOptions>(this IServiceCollection services, Func<TOptions, bool> validate, string failureMessage) where TOptions : class;
    public static IServiceCollection ValidateContextualOptions<TOptions>(this IServiceCollection services, string name, Func<TOptions, bool> validate, string failureMessage) where TOptions : class;
}
