// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.Latency;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public partial class AcceptanceTests
{
    [SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Needed for reflection")]
    private class TestStartupWithoutRedaction
    {
        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void ConfigureServices(IServiceCollection services)
            => services.AddRouting();

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void Configure(IApplicationBuilder app)
            => TestStartup.Configure(app);
    }

    [SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Needed for reflection")]
    private class TestStartupWithLatencyTelemetry
    {
        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void Configure(IApplicationBuilder app)
            => app
                .UseRequestCheckpoint()
                .UseHttpLogging()
                .UseRequestLatencyTelemetry()
                .Run(static context => context.Response.WriteAsync("Server: hello!"));
    }

    [Fact]
    public async Task AddHttpLoggingRedaction_WithoutRedactionRegistration_LogsRequest()
    {
        await RunWithoutRedactionRegistrationAsync(
            static services => services.AddHttpLoggingRedaction(),
            static async (logCollector, client) =>
            {
                using var response = await client.GetAsync("/");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal("200", logCollector.LatestRecord.GetStructuredStateValue(HttpLoggingTagNames.StatusCode));
            });
    }

    [Fact]
    public async Task AddHttpLogEnricher_WithoutRedactionRegistration_LogsEnrichedRequest()
    {
        await RunWithoutRedactionRegistrationAsync(
            static services => services.AddHttpLogEnricher<TestHttpLogEnricher>(),
            static async (logCollector, client) =>
            {
                using var response = await client.GetAsync("/");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                var record = logCollector.LatestRecord;
                Assert.Equal("200", record.GetStructuredStateValue(HttpLoggingTagNames.StatusCode));
                Assert.Equal(TestHttpLogEnricher.Value1, record.GetStructuredStateValue(TestHttpLogEnricher.Key1));
            });
    }

    [Fact]
    public async Task AddHttpLoggingRedaction_WithoutRedactionRegistration_ErasesClassifiedData()
    {
        await RunWithoutRedactionRegistrationAsync(
            static services => services.AddHttpLoggingRedaction(static options =>
            {
                options.RequestHeadersDataClasses.Add("X-Private", FakeTaxonomy.PrivateData);
                options.RequestHeadersDataClasses.Add("X-Public", DataClassification.None);
            }),
            static async (logCollector, client) =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/");
                request.Headers.Add("X-Private", "private-value");
                request.Headers.Add("X-Public", "public-value");
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                var record = logCollector.LatestRecord;
                Assert.Equal(string.Empty, record.GetStructuredStateValue(HttpLoggingTagNames.RequestHeaderPrefix + "x-private"));
                Assert.Equal("public-value", record.GetStructuredStateValue(HttpLoggingTagNames.RequestHeaderPrefix + "x-public"));
            });
    }

    [Fact]
    public async Task AddHttpLoggingRedaction_RedactionConfiguredAfterLogging_UsesConfiguredRedactor()
    {
        await RunWithoutRedactionRegistrationAsync(
            static services => services
                .AddHttpLoggingRedaction(static options => options.RequestHeadersDataClasses.Add("X-Test", FakeTaxonomy.PrivateData))
                .AddRedaction(static redaction => redaction.SetFakeRedactor(static options => options.RedactionFormat = "REDACTED:{0}", FakeTaxonomy.PrivateData)),
            static async (logCollector, client) =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/");
                request.Headers.Add("X-Test", "header-value");
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal("REDACTED:header-value", logCollector.LatestRecord.GetStructuredStateValue(HttpLoggingTagNames.RequestHeaderPrefix + "x-test"));
            });
    }

    [Fact]
    public async Task LatencyInfo_IsPopulated_WithoutLatencyContextAndRedactionRegistration()
    {
        await RunAsync<TestStartupWithLatencyTelemetry>(
            LogLevel.Information,
            static services => services
                .AddRequestLatencyTelemetry()
                .AddRequestCheckpoint()
                .AddHttpServerLatencyTelemetry(),
            static async (logCollector, client, _) =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/");
                request.Headers.Add(TelemetryConstants.ClientApplicationNameHeader, "TestClient");
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                var latencyInfo = logCollector.LatestRecord.GetStructuredStateValue("LatencyInfo");
                Assert.StartsWith("v1.0,TestClient,", latencyInfo);
                Assert.Contains(RequestCheckpointConstants.ElapsedTillPipelineExitMiddleware, latencyInfo);
            },
            fakeRedaction: false);
    }

    private static Task RunWithoutRedactionRegistrationAsync(Action<IServiceCollection> configure, Func<FakeLogCollector, HttpClient, Task> func)
        => RunAsync<TestStartupWithoutRedaction>(LogLevel.Information, configure, (collector, client, _) => func(collector, client), fakeRedaction: false);
}
#endif
