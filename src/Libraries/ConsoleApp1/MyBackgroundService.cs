using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.R9.Extensions.Metering;

namespace SampleService.Cosmic.Linux;
internal class MyBackgroundService : BackgroundService
{
    private readonly Counter<long> _counter;

    public MyBackgroundService(Meter meter)
    {
        _counter = meter.CreateCounter<long>("mycounter");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _counter.Add(1);
            await Task.Delay(1000);
        }
    }
}
