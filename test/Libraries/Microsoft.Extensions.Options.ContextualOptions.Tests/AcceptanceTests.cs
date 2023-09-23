// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Xunit;

namespace Microsoft.Extensions.Options.Contextual.Test;

#pragma warning disable SA1402 // File may only contain a single type

[OptionsContext]
internal partial class WeatherForecastContext // Note class must be partial
{
    public Guid UserId { get; set; }
    public string? Country { get; set; }
}

internal class WeatherForecastOptions
{
    public string TemperatureScale { get; set; } = "Celcius"; // Celcius or Farenheit
    public int ForecastDays { get; set; }
}

internal class CountryContextReceiver : IOptionsContextReceiver
{
    public string? Country { get; private set; }

    public void Receive<T>(string key, T value)
    {
        if (key == nameof(Country))
        {
            Country = value?.ToString();
        }
    }
}

internal class WeatherForecastService : IWeatherForecastService
{
    private readonly IContextualOptions<WeatherForecastOptions> _contextualOptions;
    private readonly Random _rng = new(0);

    public WeatherForecastService(IContextualOptions<WeatherForecastOptions> contextualOptions)
    {
        _contextualOptions = contextualOptions;
    }

    public async Task<IEnumerable<WeatherForecast>> GetForecast(WeatherForecastContext context, CancellationToken cancellationToken)
    {
        WeatherForecastOptions options = await _contextualOptions.GetAsync(context, cancellationToken).ConfigureAwait(false);
        return Enumerable.Range(1, options.ForecastDays).Select(index => new WeatherForecast
        {
            Date = new DateTime(2000, 1, 1).AddDays(index),
            Temperature = _rng.Next(-20, 55),
            TemperatureScale = options.TemperatureScale,
        });
    }
}

internal interface IWeatherForecastService
{
    Task<IEnumerable<WeatherForecast>> GetForecast(WeatherForecastContext context, CancellationToken cancellationToken);
}

internal class WeatherForecast
{
    public DateTime Date { get; set; }
    public int Temperature { get; set; }
    public string TemperatureScale { get; set; } = string.Empty;
}

public class AcceptanceTests
{
    [Fact]
    public async Task Foo()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .Configure<WeatherForecastOptions>(options => options.ForecastDays = 7)
                .AddContextualOptions()
                .Configure<WeatherForecastOptions>(ConfigureTemperatureScaleBasedOnCountry)
                .AddSingleton<IWeatherForecastService, WeatherForecastService>())
                .Build();

        var forecastService = host
                .Services
                .GetRequiredService<IWeatherForecastService>();

        Assert.Equal("Farenheit", (await forecastService.GetForecast(new WeatherForecastContext { Country = "US" }, CancellationToken.None)).First().TemperatureScale);
        Assert.Equal("Celcius", (await forecastService.GetForecast(new WeatherForecastContext { Country = "CA" }, CancellationToken.None)).First().TemperatureScale);

        static void ConfigureTemperatureScaleBasedOnCountry(IOptionsContext context, WeatherForecastOptions options)
        {
            CountryContextReceiver receiver = new();
            context.PopulateReceiver(receiver);
            if (receiver.Country == "US")
            {
                options.TemperatureScale = "Farenheit";
            }
        }
    }
}
