using System;
using System.Linq;
using System.Collections;
using System.Reflection;

namespace Microsoft.AspNet.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTransient<TService, TImplementation>([NotNull]this IServiceCollection services)
        {
            return services.AddTransient(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddTransient([NotNull]this IServiceCollection services, Type serviceType)
        {
            return services.AddTransient(serviceType, serviceType);
        }

        public static IServiceCollection AddTransient<TService>([NotNull]this IServiceCollection services)
        {
            return services.AddTransient(typeof(TService));
        }

        public static IServiceCollection AddScoped<TService, TImplementation>([NotNull]this IServiceCollection services)
        {
            return services.AddScoped(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddScoped([NotNull]this IServiceCollection services, Type serviceType)
        {
            return services.AddScoped(serviceType, serviceType);
        }

        public static IServiceCollection AddScoped<TService>([NotNull]this IServiceCollection services)
        {
            return services.AddScoped(typeof(TService));
        }

        public static IServiceCollection AddSingleton<TService, TImplementation>([NotNull]this IServiceCollection services)
        {
            return services.AddSingleton(typeof(TService), typeof(TImplementation));
        }

        public static IServiceCollection AddSingleton([NotNull]this IServiceCollection services, Type serviceType)
        {
            return services.AddSingleton(serviceType, serviceType);
        }

        public static IServiceCollection AddSingleton<TService>([NotNull]this IServiceCollection services)
        {
            return services.AddSingleton(typeof(TService));
        }

        public static IServiceCollection AddInstance<TService>([NotNull]this IServiceCollection services, TService implementationInstance)
        {
            return services.AddInstance(typeof(TService), implementationInstance);
        }

        public static IServiceCollection AddSetup([NotNull]this IServiceCollection services, Type setupType)
        {
            var serviceTypes = setupType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof (IOptionsSetup<>));
            foreach (var serviceType in serviceTypes)
            {
                services.Add(new ServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationType = setupType,
                    Lifecycle = LifecycleKind.Transient
                });
            }
            return services;
        }

        public static IServiceCollection AddSetup<TSetup>([NotNull]this IServiceCollection services)
        {
            return services.AddSetup(typeof (TSetup));
        }

        public static IServiceCollection AddSetup([NotNull]this IServiceCollection services, [NotNull]object setupInstance)
        {
            var setupType = setupInstance.GetType();
            var serviceTypes = setupType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof (IOptionsSetup<>));
            foreach (var serviceType in serviceTypes)
            {
                services.Add(new ServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationInstance = setupInstance,
                    Lifecycle = LifecycleKind.Singleton
                });
            }
            return services;
        }


        public static IServiceCollection SetupOptions<TOptions>([NotNull]this IServiceCollection services,
            Action<TOptions> setupAction,
            int order)
        {
            services.AddSetup(new OptionsSetup<TOptions>(setupAction) {Order = order});
            return services;
        }

        public static IServiceCollection SetupOptions<TOptions>([NotNull]this IServiceCollection services,
            Action<TOptions> setupAction)
        {
            return services.SetupOptions<TOptions>(setupAction, order: 0);
        }
    }
}
