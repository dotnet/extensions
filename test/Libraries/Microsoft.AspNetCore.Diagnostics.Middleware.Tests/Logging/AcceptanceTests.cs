// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;
using Microsoft.Shared.Text;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public partial class AcceptanceTests
{
    private const string LoggingCategory = "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware";
    private const int ErrorRouteProcessingTimeMs = 1_000;
    private const int SlashRouteProcessingTimeMs = 2_000;
    private static readonly TimeSpan _defaultLogTimeout = TimeSpan.FromSeconds(5);

    [SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Needed for reflection")]
    private class TestStartup
    {
        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddFakeRedaction();
            services.AddHttpLoggingRedaction();
        }

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseHttpLogging();

            app.Map("/error", static x =>
                x.Run(static async context =>
                {
                    await context.Request.Body.DrainAsync(default);

                    if (context.Request.QueryString.HasValue)
                    {
                        if (context.Request.QueryString.Value!.Contains("status"))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        }

                        if (context.Request.QueryString.Value!.Contains("body"))
                        {
                            context.Response.ContentType = MediaTypeNames.Text.Plain;
                            await context.Response.WriteAsync("test body");
                        }
                    }

                    var fakeTimeProvider = context.RequestServices.GetRequiredService<FakeTimeProvider>();
                    fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(ErrorRouteProcessingTimeMs));
                    throw new InvalidOperationException("Test exception");
                }));

            app.Run(static async context =>
            {
                var fakeTimeProvider = context.RequestServices.GetRequiredService<FakeTimeProvider>();
                fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(SlashRouteProcessingTimeMs));

                await context.Request.Body.DrainAsync(default).ConfigureAwait(false);

                context.Response.ContentType = MediaTypeNames.Text.Plain;
                context.Response.Headers.Append(HeaderNames.TransferEncoding, "chunked");
                await context.Response.WriteAsync("Server: hello!").ConfigureAwait(false);

                // Writing response twice so header is sent as 'transfer chunked-encoding'
                await context.Response.WriteAsync("Server: world!").ConfigureAwait(false);
            });
        }
    }

    private static Task RunAsync(LogLevel level, Action<IServiceCollection> configure, Func<FakeLogCollector, HttpClient, Task> func)
        => RunAsync<TestStartup>(level, configure, (collector, client, _) => func(collector, client));

    private static async Task RunAsync<TStartup>(
        LogLevel level,
        Action<IServiceCollection> configure,
        Func<FakeLogCollector, HttpClient, IServiceProvider, Task> func)
        where TStartup : class
    {
        using var host = await FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(loggingBuilder => loggingBuilder
                .AddFilter("Microsoft.Hosting", LogLevel.Warning)
                .AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning)
                .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
                .AddFilter("Microsoft.AspNetCore.HttpLogging", level)
                .SetMinimumLevel(level)
                .AddFakeLogging())
            .ConfigureServices(x =>
            {
                x.AddSingleton<FakeTimeProvider>();
                x.AddSingleton<TimeProvider>(s => s.GetRequiredService<FakeTimeProvider>());
            })
            .ConfigureWebHost(static builder => builder
                .UseStartup<TStartup>()
                .UseTestServer())
            .ConfigureServices(configure)
            .StartAsync();

        var logCollector = host.Services.GetFakeLogCollector();

        using var client = host.GetTestClient();

        await func(logCollector, client, host.Services).ConfigureAwait(false);
        await host.StopAsync();
    }

    private static async Task WaitForLogRecordsAsync(FakeLogCollector logCollector, TimeSpan timeout, int expectedRecords = 1)
    {
        var totalTimeWaiting = TimeSpan.Zero;
        var spinTime = TimeSpan.FromMilliseconds(50);
        while (totalTimeWaiting < timeout)
        {
            if (logCollector.Count >= expectedRecords)
            {
                return;
            }

            await Task.Delay(spinTime);
            totalTimeWaiting += spinTime;
        }

        throw new TimeoutException("No log records were emitted, timeout was reached");
    }

    [Theory]
    [InlineData(MediaTypeNames.Text.Plain, true)]
    [InlineData(MediaTypeNames.Text.Html, false)]
    [InlineData(MediaTypeNames.Text.RichText, false)]
    [InlineData(MediaTypeNames.Text.Xml, false)]
    public async Task HttpLogging_WhenLogLevelInfo_LogResponseBody(string responseContentTypeToLog, bool shouldLog)
    {
        await RunAsync(
            LogLevel.Information,
            services => services.AddHttpLogging(x =>
            {
                x.MediaTypeOptions.Clear();
                x.MediaTypeOptions.AddText(responseContentTypeToLog);
                x.LoggingFields |= HttpLoggingFields.ResponseBody;
            }).AddHttpLoggingRedaction(),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content);
                using var response = await client.PostAsync("/", content).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal(1, logCollector.Count);
                Assert.Null(logCollector.LatestRecord.Exception);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logCollector.LatestRecord.StructuredState!;

                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Post.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == SlashRouteProcessingTimeMs);

                if (shouldLog)
                {
                    Assert.Single(state, x => x.Key == HttpLoggingTagNames.ResponseBody && x.Value == "Server: hello!Server: world!");
                    Assert.Equal(8, state!.Count);
                }
                else
                {
                    Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
                    Assert.Equal(7, state!.Count);
                }
            });
    }

    [Theory]
    [InlineData(MediaTypeNames.Text.Plain, true)]
    [InlineData(MediaTypeNames.Text.Html, true)]
    [InlineData(MediaTypeNames.Text.RichText, true)]
    [InlineData(MediaTypeNames.Text.Xml, true)]
    [InlineData(MediaTypeNames.Application.Json, false)]
    [InlineData(MediaTypeNames.Application.Xml, false)]
    [InlineData(MediaTypeNames.Image.Jpeg, false)]
    public async Task HttpLogging_WhenLogLevelInfo_LogRequestBody(string requestContentType, bool shouldLog)
    {
        await RunAsync(
            LogLevel.Information,
            services => services.AddHttpLogging(x =>
            {
                x.MediaTypeOptions.Clear();
                x.MediaTypeOptions.AddText("text/*");
                x.LoggingFields |= HttpLoggingFields.RequestBody;
            }).AddHttpLoggingRedaction(),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content, null, requestContentType);
                using var response = await client.PostAsync("/", content).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal(1, logCollector.Count);
                Assert.Null(logCollector.LatestRecord.Exception);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logCollector.LatestRecord.StructuredState!;

                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Post.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == SlashRouteProcessingTimeMs);

                if (shouldLog)
                {
                    Assert.Single(state, x => x.Key == HttpLoggingTagNames.RequestBody && x.Value == Content);
                    Assert.Equal(9, state!.Count);
                }
                else
                {
                    Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                    Assert.Equal(7, state!.Count);
                }
            });
    }

    [Fact]
    public async Task HttpLogging_WhenLogLevelInfo_LogRequestStart()
    {
        await RunAsync(
            LogLevel.Information,
            static services => services.AddHttpLoggingRedaction(x =>
            {
                x.RequestHeadersDataClasses.Add(HeaderNames.Accept, DataClassification.None);
                x.ResponseHeadersDataClasses.Add(HeaderNames.TransferEncoding, DataClassification.None);
            }).AddHttpLogging(static x =>
            {
                x.CombineLogs = false;
                x.LoggingFields = HttpLoggingFields.All;
                x.MediaTypeOptions.AddText(MediaTypeNames.Text.Plain);
            }),
            async static (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                const string NormalizedRequestHeader = "accept";
                const string NormalizedResponseHeader = "transfer-encoding";

                using var request = new HttpRequestMessage(HttpMethod.Post, "/")
                {
                    Content = new StringContent(Content)
                };

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
                using var response = await client.SendAsync(request).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                var logRecords = logCollector.GetSnapshot();
                Assert.Equal(5, logRecords.Count);
                Assert.All(logRecords, x => Assert.Null(x.Exception));
                Assert.All(logRecords, x => Assert.Equal(LogLevel.Information, x.Level));
                Assert.All(logRecords, x => Assert.Equal(LoggingCategory, x.Category));

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var requestState = logRecords[0].StructuredState;
                var requestBodyState = logRecords[1].StructuredState;
                var responseState = logRecords[2].StructuredState;
                var responseBodyState = logRecords[3].StructuredState;
                var durationState = logRecords[4].StructuredState;

                Assert.Equal(6, requestState!.Count);
                Assert.DoesNotContain(requestState, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.DoesNotContain(requestState, x => x.Key == HttpLoggingTagNames.StatusCode);
                Assert.DoesNotContain(requestState, x => x.Key == HttpLoggingTagNames.Duration);
                Assert.Single(requestState, x => x.Key == HttpLoggingTagNames.RequestHeaderPrefix + NormalizedRequestHeader);
                Assert.Single(requestState, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(requestState, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(requestState, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Post.ToString());
                Assert.Single(requestState, x => x.Key == "Protocol" && x.Value == "HTTP/1.1");

                Assert.Equal(3, requestBodyState!.Count);
                Assert.Single(requestBodyState, x => x.Key == "Body" && x.Value == Content);

                Assert.Equal(2, responseState!.Count);
                Assert.Single(responseState, x => x.Key == HttpLoggingTagNames.ResponseHeaderPrefix + NormalizedResponseHeader);
                Assert.Single(responseState, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);

                Assert.Equal(2, responseBodyState!.Count);
                Assert.Single(responseBodyState, x => x.Key == "Body" && x.Value == "Server: hello!Server: world!");

                Assert.Equal(2, durationState!.Count);
                Assert.Single(durationState, x => x.Key == HttpLoggingTagNames.Duration && x.Value != null);
            });
    }

    [Fact]
    public async Task HttpLogging_WhenLogLevelInfo_LogHeaders()
    {
        await RunAsync(
            LogLevel.Information,
            static services => services.AddHttpLoggingRedaction(static x =>
            {
                x.RequestHeadersDataClasses.Add(HeaderNames.Accept, DataClassification.None);
                x.ResponseHeadersDataClasses.Add(HeaderNames.TransferEncoding, DataClassification.None);
            }),
            async static (logCollector, client) =>
            {
                const string NormalizedRequestHeader = "accept";
                const string NormalizedResponseHeader = "transfer-encoding";

                using var httpMessage = new HttpRequestMessage(HttpMethod.Get, "/");
                httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

                using var response = await client.SendAsync(httpMessage).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal(1, logCollector.Count);

                var lastRecord = logCollector.LatestRecord;
                Assert.Null(lastRecord.Exception);
                Assert.Equal(LogLevel.Information, lastRecord.Level);
                Assert.Equal(LoggingCategory, lastRecord.Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = lastRecord.StructuredState;

                Assert.Equal(9, state!.Count);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.RequestHeaderPrefix + NormalizedRequestHeader);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.ResponseHeaderPrefix + NormalizedResponseHeader);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == SlashRouteProcessingTimeMs);

                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Get.ToString());
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.DoesNotContain(state, x =>
                    x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix) && !x.Key.EndsWith(NormalizedRequestHeader));

                Assert.DoesNotContain(state, x =>
                    x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix) && !x.Key.EndsWith(NormalizedResponseHeader));
            });
    }

    [Fact]
    public async Task HttpLogging_WhenEnricherAdded_LogAdditionalProps()
    {
        await RunAsync(
            LogLevel.Information,
            static x =>
            {
                x.AddHttpLogEnricher<TestHttpLogEnricher>();
                x.AddHttpLoggingRedaction();
            },
            async static (logCollector, client) =>
            {
                using var response = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal(1, logCollector.Count);
                Assert.Null(logCollector.LatestRecord.Exception);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(9, state!.Count);
                Assert.Single(state, x => x.Key == TestHttpLogEnricher.Key1 && x.Value == TestHttpLogEnricher.Value1);
                Assert.Single(state, x => x.Key == TestHttpLogEnricher.Key2 && x.Value == TestHttpLogEnricher.Value2.ToString(CultureInfo.InvariantCulture));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Delete.ToString());
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
            });
    }

    [Theory]
    [InlineData(IncomingPathLoggingMode.Structured)]
    [InlineData(IncomingPathLoggingMode.Formatted)]
    public async Task HttpLogging_WhenRedactionModeNone_LogIncomingRequestPath(IncomingPathLoggingMode pathLoggingMode)
    {
        await RunAsync(
            LogLevel.Information,
            x =>
            {
                x.AddHttpLoggingRedaction(options =>
                {
                    options.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None;
                    options.RequestPathLoggingMode = pathLoggingMode;
                });
            },
            async static (logCollector, client) =>
            {
                const string RequestPath = "/api/users/123/add-task/345";
                using var response = await client.GetAsync(RequestPath).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal(1, logCollector.Count);
                Assert.Null(logCollector.LatestRecord.Exception);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(7, state!.Count);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Path && x.Value == RequestPath);
            });
    }

    [Fact]
    public async Task HttpLogging_WhenLogRequestStart_SkipEnrichingFirstLogRecord()
    {
        await RunAsync(
            LogLevel.Information,
            static x =>
            {
                x.AddHttpLogEnricher<TestHttpLogEnricher>();
                x.AddHttpLoggingRedaction().AddHttpLogging(x => x.CombineLogs = false);
            },
            async static (logCollector, client) =>
            {
                using var response = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                var logRecords = logCollector.GetSnapshot();
                Assert.Equal(3, logRecords.Count);
                Assert.All(logRecords, x => Assert.Null(x.Exception));
                Assert.All(logRecords, x => Assert.Equal(LogLevel.Information, x.Level));
                Assert.All(logRecords, x => Assert.Equal(LoggingCategory, x.Category));

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var firstState = logRecords[0].StructuredState;
                var secondState = logRecords[1].StructuredState;

                Assert.Equal(5, firstState!.Count);
                Assert.DoesNotContain(firstState, x => x.Key == TestHttpLogEnricher.Key1 && x.Value == TestHttpLogEnricher.Value1);
                Assert.DoesNotContain(firstState, x => x.Key == TestHttpLogEnricher.Key2 && x.Value == TestHttpLogEnricher.Value2.ToString(CultureInfo.InvariantCulture));

                Assert.Equal(3, secondState!.Count);
                Assert.Single(secondState, x => x.Key == TestHttpLogEnricher.Key1 && x.Value == TestHttpLogEnricher.Value1);
                Assert.Single(secondState, x => x.Key == TestHttpLogEnricher.Key2 && x.Value == TestHttpLogEnricher.Value2.ToString(CultureInfo.InvariantCulture));
            });
    }

    [Fact]
    public async Task HttpLogging_WhenSecondLogRequestStart_DontLogDurationAndStatus()
    {
        await RunAsync(
            LogLevel.Information,
            static x => x.AddHttpLoggingRedaction().AddHttpLogging(x => x.CombineLogs = false),
            async static (logCollector, client) =>
            {
                using var firstResponse = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.True(firstResponse.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                using var secondResponse = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.True(secondResponse.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 4);

                var logRecords = logCollector.GetSnapshot();
                Assert.Equal(6, logRecords.Count);
                Assert.All(logRecords, x => Assert.Null(x.Exception));
                Assert.All(logRecords, x => Assert.Equal(LogLevel.Information, x.Level));
                Assert.All(logRecords, x => Assert.Equal(LoggingCategory, x.Category));

                var responseStatus = ((int)firstResponse.StatusCode).ToInvariantString();
                var firstRecord = logRecords[0].StructuredState;
                var secondRecord = logRecords[1].StructuredState;
                var thirdRecord = logRecords[2].StructuredState;
                var fourthRecord = logRecords[3].StructuredState;
                var fithRecord = logRecords[4].StructuredState;
                var sixthRecord = logRecords[5].StructuredState;

                Assert.Equal(5, firstRecord!.Count);
                Assert.Single(secondRecord!);
                Assert.Equal(5, fourthRecord!.Count);
                Assert.Single(fithRecord!);
                Assert.DoesNotContain(firstRecord, x => x.Key == HttpLoggingTagNames.StatusCode);
                Assert.DoesNotContain(firstRecord, x => x.Key == HttpLoggingTagNames.Duration);
                Assert.DoesNotContain(secondRecord!, x => x.Key == HttpLoggingTagNames.Duration);
                Assert.DoesNotContain(fourthRecord, x => x.Key == HttpLoggingTagNames.StatusCode);
                Assert.DoesNotContain(fourthRecord, x => x.Key == HttpLoggingTagNames.Duration);
                Assert.DoesNotContain(fithRecord!, x => x.Key == HttpLoggingTagNames.Duration);

                Assert.Single(secondRecord!);
                Assert.Single(fithRecord!);
                Assert.Single(secondRecord!, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(fithRecord!, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);

                Assert.Equal(2, thirdRecord!.Count);
                Assert.Equal(2, sixthRecord!.Count);
                Assert.Single(thirdRecord, x => x.Key == HttpLoggingTagNames.Duration && x.Value != null);
                Assert.Single(sixthRecord, x => x.Key == HttpLoggingTagNames.Duration && x.Value != null);
            });
    }

    [Theory]
    [InlineData("/error", "200")]
    [InlineData("/error?status=1", "400")]
    public async Task HttpLogging_WhenException_LogError(string requestPath, string expectedStatus)
    {
        await RunAsync(
            LogLevel.Information,
            static services => services.AddHttpLoggingRedaction(),
            async (logCollector, client) =>
            {
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync(requestPath));
                Assert.Equal("Test exception", ex.Message);

                Assert.Equal(1, logCollector.Count);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);
                Assert.Null(logCollector.LatestRecord.Exception);

                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(7, state!.Count);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Get.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == expectedStatus);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == ErrorRouteProcessingTimeMs);
            });
    }

    [Fact]
    public async Task HttpLogging_WhenException_LogBody()
    {
        await RunAsync(
            LogLevel.Information,
            static services => services.AddHttpLogging(static x =>
            {
                x.MediaTypeOptions.AddText(MediaTypeNames.Text.Plain);
                x.LoggingFields |= HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody;
            }).AddHttpLoggingRedaction(),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content);
                var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.PutAsync("/error?body=true", content));

                var originalException = ex.InnerException?.InnerException;
                Assert.NotNull(originalException);
                Assert.Equal("Test exception", originalException!.Message);

                Assert.Equal(1, logCollector.Count);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);
                Assert.Null(logCollector.LatestRecord.Exception);

                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(10, state!.Count);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Put.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.RequestBody && x.Value == Content);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.ResponseBody && x.Value == "test body");
            });
    }

    [Fact]
    public async Task HttpLogging_WhenException_DontLogResponseBody()
    {
        await RunAsync(
            LogLevel.Information,
            static services => services.AddHttpLogging(static x => x.LoggingFields &= ~HttpLoggingFields.ResponseBody)
                .AddHttpLoggingRedaction(),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content);
                var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.PutAsync("/error?body=true", content));

                var originalException = ex.InnerException?.InnerException;
                Assert.NotNull(originalException);
                Assert.Equal("Test exception", originalException!.Message);

                Assert.Equal(1, logCollector.Count);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);
                Assert.Null(logCollector.LatestRecord.Exception);

                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(7, state!.Count);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Put.ToString());
            });
    }

    [Fact]
    public async Task HttpLogging_WhenLogLevelError_NoLogHttp()
    {
        await RunAsync(
            LogLevel.Error,
            static _ => { },
            async static (logCollector, client) =>
            {
                using var content = new StringContent("Client: hello!");
                using var response = await client.PostAsync("/", content).ConfigureAwait(false);

                Assert.True(response.IsSuccessStatusCode);

                Assert.Equal(0, logCollector.Count);
            });
    }

    [Theory]
    [InlineData("/home/api", "/home", true)]
    [InlineData("/HOME/API", "/home/api", true)]
    [InlineData("/home/api", "/home/users", false)]
    [InlineData("/Home/Chats", "/home/chats", true)]
    [InlineData("/home/chats/123", "/home/chats", true)]
    [InlineData("/home/users/", "/home", true)]
    [InlineData("/HOME/users", "/home", true)]
    [InlineData("/home/users/foo", "/home/api", false)]
    [InlineData("/", "/home", false)]
    [InlineData("", "/home", false)]
    [InlineData("/home", "/", true)]
    public async Task HttpLogging_LogRecordIsNotCreated_If_isFiltered_True(string httpPath, string excludedPath, bool isFiltered)
    {
        await RunAsync(
            LogLevel.Information,
            services => services.AddHttpLoggingRedaction(x =>
            {
                x.ExcludePathStartsWith.Add(excludedPath);
            }),
            async (logCollector, client) =>
            {
                using var response = await client.GetAsync(httpPath).ConfigureAwait(false);

                Assert.True(response.IsSuccessStatusCode);

                if (isFiltered)
                {
                    Assert.Equal(0, logCollector.Count);
                }
                else
                {
                    await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);
                    Assert.Equal(1, logCollector.Count);
                    Assert.Equal(7, logCollector.GetSnapshot()[0].StructuredState!.Count);
                }
            });
    }

    [Fact]
    public async Task HttpLogging_LogRecordIsNotCreated_If_Disabled()
    {
        await RunAsync(
            LogLevel.Information,
            services => services.AddHttpLoggingRedaction(x =>
            {
            }).AddHttpLogging(o =>
            {
                o.LoggingFields = HttpLoggingFields.None;
            }),
            async (logCollector, client) =>
            {
                using var response = await client.GetAsync("").ConfigureAwait(false);

                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(0, logCollector.Count);
            });
    }

    [Fact]
    public async Task HttpLogging_EnricherThrows_Logged()
    {
        await RunAsync(
            LogLevel.Debug,
            services => services.AddHttpLoggingRedaction()
            .AddHttpLogEnricher<ThrowingEnricher>(),
            async (logCollector, client) =>
            {
                using var response = await client.GetAsync("").ConfigureAwait(false);

                Assert.True(response.IsSuccessStatusCode);
                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);
                Assert.Equal(2, logCollector.Count);
                Assert.IsType<InvalidOperationException>(logCollector.GetSnapshot()[0].Exception);
                Assert.Equal(7, logCollector.GetSnapshot()[1].StructuredState!.Count);
            });
    }

    private class ThrowingEnricher : IHttpLogEnricher
    {
        public void Enrich(IEnrichmentTagCollector collector, HttpContext httpContext) => throw new InvalidOperationException();
    }
}
#endif
