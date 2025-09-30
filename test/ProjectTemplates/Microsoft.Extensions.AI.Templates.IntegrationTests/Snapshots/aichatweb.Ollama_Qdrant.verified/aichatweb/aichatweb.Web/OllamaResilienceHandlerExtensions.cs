using System;
using Microsoft.Extensions.DependencyInjection;

namespace aichatweb.Web.Services;

public static class OllamaResilienceHandlerExtensions
{
    public static IServiceCollection AddOllamaResilienceHandler(this IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(http =>
        {
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental
            http.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

            // Turn on resilience by default
            http.AddStandardResilienceHandler(config =>
            {
                // Extend the HTTP Client timeout for Ollama
                config.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);

                // Must be at least double the AttemptTimeout to pass options validation
                config.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
                config.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
            });

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return services;
    }
}

