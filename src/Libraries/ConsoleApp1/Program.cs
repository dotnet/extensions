// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.R9.Extensions.Hosting.Cosmic;
using Microsoft.R9.Extensions.Metering;
using SampleService.Cosmic.Linux;

#pragma warning disable R9EXP0025 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable R9EXPDEV // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var listener = new MeterListener();

// Callback when a new instrument is published
listener.InstrumentPublished = (instrument, listener) =>
{
    Console.WriteLine($"Instrument published: {instrument.Name} from {instrument.Meter.Name}");
    // Enable the instrument for this listener
    listener.EnableMeasurementEvents(instrument);
};
// Callback when measurements are recorded
listener.MeasurementsCompleted = (instrument, state) =>
{
    Console.WriteLine($"Finished listening to {instrument.Name}");
};

listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
{

});

// Start the listener
listener.Start();

await CosmicHost.CreateBuilderV2([
        "--config-path=appsettings.json",
                "--no-secrets",
                "--development"
     ])
    .ConfigureServices((context, sc) =>
    {
        sc.AddSingleton(x => new Meter("mymeter"));
        sc.AddResourceMonitoring();
        // _ = sc.AddHostedService<MyBackgroundService>();
    })
    .Build()
    .RunAsync();
