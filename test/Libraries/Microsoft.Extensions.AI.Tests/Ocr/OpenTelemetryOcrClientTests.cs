// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryOcrClientTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new OpenTelemetryOcrClient(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpectedInformationLogged_Async(bool enableSensitiveData)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestOcrClient
        {
            ExtractAsyncCallback = async (document, mediaType, options, progress, cancellationToken) =>
            {
                await Task.Yield();
                return new OcrResult([new OcrPage(1, "This is the recognized text.")])
                {
                    ModelId = "amazingmodel",
                    Usage = new() { PagesProcessed = 3 },
                };
            },

            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(OcrClientMetadata) ? new OcrClientMetadata("testservice", new Uri("http://localhost:12345/something"), "amazingmodel") :
                null,
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(null, sourceName, configure: instance =>
            {
                instance.EnableSensitiveData = enableSensitiveData;
            })
            .Build();

        OcrOptions options = new()
        {
            ModelId = "mycoolocrmodel",
            AdditionalProperties = new()
            {
                ["service_tier"] = "value1",
                ["SomethingElse"] = "value2",
            },
        };

        _ = await client.ExtractAsync(Stream.Null, "application/pdf", options);

        var activity = Assert.Single(activities);

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal("generate_content mycoolocrmodel", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.provider.name"));

        Assert.Equal("mycoolocrmodel", activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(enableSensitiveData ? "value1" : null, activity.GetTagItem("service_tier"));
        Assert.Equal(enableSensitiveData ? "value2" : null, activity.GetTagItem("SomethingElse"));

        Assert.Equal("amazingmodel", activity.GetTagItem("gen_ai.response.model"));
        Assert.Equal(3, (int)activity.GetTagItem("gen_ai.usage.pages_processed")!);

        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }

    [Fact]
    public void GetService_ReturnsActivitySource()
    {
        using var innerClient = new TestOcrClient();
        using var client = innerClient.AsBuilder().UseOpenTelemetry().Build();

        Assert.NotNull(client.GetService<ActivitySource>());
    }
}
