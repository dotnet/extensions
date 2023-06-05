// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Shared.Text;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public partial class AcceptanceTest
{
    private const string RedactedFormat = "<redacted:{0}>";
    private const string UserIdParamName = "userId";
    private const string NoDataClassParamName = "noDataClassification";
    private const string QueryParamName = "noRedaction";

    internal const string ActionRouteTemplate = "api/users/{userId}/{noDataClassification}";
    internal const int ControllerProcessingTimeMs = 1_000;

    private class TestStartupWithControllers
    {
        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void ConfigureServices(IServiceCollection services)
            => services
                .AddFakeRedaction(x => x.RedactionFormat = RedactedFormat)
                .AddRouting() // Adds routing middleware.
                .AddControllers(); // Allows to read routes from classes annotated with [ApiController] attribute.

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void Configure(IApplicationBuilder app)
            => app
                .UseRouting()
                .UseHttpLoggingMiddleware()
                .UseEndpoints(endpoints => endpoints.MapControllers());
    }

    private static Task RunControllerAsync(LogLevel level, Action<IServiceCollection> configure, Func<FakeLogCollector, HttpClient, Task> func)
        => RunAsync<TestStartupWithControllers>(level, configure, (collector, client, _) => func(collector, client));

    [Theory]
    [InlineData(HttpRouteParameterRedactionMode.Strict, $"api/users/<redacted:testUserId>/{TelemetryConstants.Redacted}")]
    [InlineData(HttpRouteParameterRedactionMode.Loose, "api/users/<redacted:testUserId>/someTestData")]
    public async Task TestServer_WhenController_RedactPath(HttpRouteParameterRedactionMode mode, string redactedPath)
    {
        await RunControllerAsync(
            LogLevel.Information,
            services => services.AddHttpLogging(o => o.RequestPathParameterRedactionMode = mode),
            async (logCollector, client) =>
            {
                const string UserId = "testUserId";
                using var response = await client.GetAsync($"/api/users/{UserId}/someTestData?{QueryParamName}=foo").ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, TimeSpan.FromSeconds(30));

                Assert.Equal(1, logCollector.Count);

                var logRecord = logCollector.LatestRecord;
                Assert.Null(logRecord.Exception);
                Assert.Equal(LoggingCategory, logRecord.Category);
                Assert.Equal(LogLevel.Information, logRecord.Level);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logRecord.StructuredState!;

                Assert.DoesNotContain(state, x => x.Key == QueryParamName);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Path && x.Value == redactedPath);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Method && x.Value == HttpMethod.Get.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == ControllerProcessingTimeMs);
            });
    }

    [Fact]
    public async Task TestServer_WhenControllerWithPathRoute_RedactParameters()
    {
        await RunControllerAsync(
            LogLevel.Information,
            services => services.AddHttpLogging(x => x.RequestPathLoggingMode = IncomingPathLoggingMode.Structured),
            async (logCollector, client) =>
            {
                const string UserId = "testUserId";
                using var response = await client.GetAsync($"/api/users/{UserId}/someTestData?{QueryParamName}=foo").ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, TimeSpan.FromSeconds(30));

                Assert.Equal(1, logCollector.Count);

                var logRecord = logCollector.LatestRecord;
                Assert.Null(logRecord.Exception);
                Assert.Equal(LoggingCategory, logRecord.Category);
                Assert.Equal(LogLevel.Information, logRecord.Level);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logRecord.StructuredState!;

                string redactedUserId = string.Format(CultureInfo.InvariantCulture, RedactedFormat, UserId);
                Assert.Single(state, x => x.Key == UserIdParamName && x.Value == redactedUserId);
                Assert.Single(state, x => x.Key == NoDataClassParamName && x.Value == TelemetryConstants.Redacted);
                Assert.DoesNotContain(state, x => x.Key == QueryParamName);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Path && x.Value == ActionRouteTemplate);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Method && x.Value == HttpMethod.Get.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == ControllerProcessingTimeMs);
            });
    }

    [Theory]
    [CombinatorialData]
    public async Task TestServer_WhenControllerWithPathRoute_HonorRouteParamDataClassMap(bool routeParameterRedactionModeNone)
    {
        await RunControllerAsync(
            LogLevel.Information,
            services => services.AddHttpLogging(x =>
            {
                x.RouteParameterDataClasses.Add(new(NoDataClassParamName, DataClassification.None));
                x.RequestPathLoggingMode = IncomingPathLoggingMode.Structured;
                x.RequestPathParameterRedactionMode = routeParameterRedactionModeNone
                    ? HttpRouteParameterRedactionMode.None : HttpRouteParameterRedactionMode.Strict;
            }),
            async (logCollector, client) =>
            {
                const string UserId = "testUserId";
                const string NoDataClassParamValue = "someTestData";
                using var response = await client.GetAsync($"/api/users/{UserId}/{NoDataClassParamValue}?{QueryParamName}=foo").ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, TimeSpan.FromSeconds(30));

                Assert.Equal(1, logCollector.Count);

                var logRecord = logCollector.LatestRecord;
                Assert.Null(logRecord.Exception);
                Assert.Equal(LoggingCategory, logRecord.Category);
                Assert.Equal(LogLevel.Information, logRecord.Level);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logRecord.StructuredState!;

                string redactedUserId = string.Format(CultureInfo.InvariantCulture, RedactedFormat, UserId);

                if (!routeParameterRedactionModeNone)
                {
                    Assert.Single(state, x => x.Key == UserIdParamName && x.Value == redactedUserId);
                    Assert.Single(state, x => x.Key == NoDataClassParamName && x.Value == NoDataClassParamValue);
                }

                Assert.DoesNotContain(state, x => x.Key == QueryParamName);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Host && !string.IsNullOrEmpty(x.Value));
                var expectedPath = routeParameterRedactionModeNone ? $"/api/users/{UserId}/{NoDataClassParamValue}" : ActionRouteTemplate;
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Path && x.Value == expectedPath);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Method && x.Value == HttpMethod.Get.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == ControllerProcessingTimeMs);
            });
    }

    [Fact]
    public async Task TestServer_WhenControllerWithPathRoute_RedactionModeNone()
    {
        await RunControllerAsync(
            LogLevel.Information,
            services => services.AddHttpLogging(x => x.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None),
            async (logCollector, client) =>
            {
                const string UserId = "testUserId";
                using var response = await client.GetAsync($"/api/users/{UserId}/someTestData?{QueryParamName}=foo").ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, TimeSpan.FromSeconds(30));

                Assert.Equal(1, logCollector.Count);

                var logRecord = logCollector.LatestRecord;
                Assert.Null(logRecord.Exception);
                Assert.Equal(LoggingCategory, logRecord.Category);
                Assert.Equal(LogLevel.Information, logRecord.Level);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logRecord.StructuredState!;

                Assert.DoesNotContain(state, x => x.Key == QueryParamName);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Path && x.Value == $"/api/users/testUserId/someTestData");
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Method && x.Value == HttpMethod.Get.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == ControllerProcessingTimeMs);
            });
    }

    [Theory]
    [CombinatorialData]
    public async Task TestServer_WhenControllerWithoutPathRoute_LogPath(bool routeParameterRedactionModeNone)
    {
        const string RequestPath = $"/api/test/1/2/3";

        await RunControllerAsync(
            LogLevel.Information,
            services => services.AddHttpLogging(x =>
            {
                x.RequestPathParameterRedactionMode = routeParameterRedactionModeNone
                    ? HttpRouteParameterRedactionMode.None : HttpRouteParameterRedactionMode.Strict;
            }),
            async (logCollector, client) =>
            {
                using var response = await client.GetAsync(RequestPath).ConfigureAwait(false);
                Assert.False(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, TimeSpan.FromSeconds(30));

                Assert.Equal(1, logCollector.Count);

                var logRecord = logCollector.LatestRecord;
                Assert.Null(logRecord.Exception);
                Assert.Equal(LoggingCategory, logRecord.Category);
                Assert.Equal(LogLevel.Information, logRecord.Level);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logRecord.StructuredState!;

                var expectedPath = routeParameterRedactionModeNone ? RequestPath : TelemetryConstants.Unknown;

                Assert.DoesNotContain(state, x => x.Key == QueryParamName);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Path && x.Value == expectedPath);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingDimensions.Method && x.Value == HttpMethod.Get.ToString());
            });
    }
}
