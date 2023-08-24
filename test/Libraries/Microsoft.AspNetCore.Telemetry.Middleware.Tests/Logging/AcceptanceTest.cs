// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET8_0_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Telemetry.Http.Logging.Test.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;
using Microsoft.Shared.Text;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public partial class AcceptanceTest
{
    private const string LoggingCategory = "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware";
    private const int ErrorRouteProcessingTimeMs = 1_000;
    private const int SlashRouteProcessingTimeMs = 2_000;
    private static readonly TimeSpan _defaultLogTimeout = TimeSpan.FromSeconds(5);

    private class TestStartup
    {
        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddFakeRedaction();
            services.AddHttpLoggingRedaction();
            services.AddSingleton<TestBodyPipeFeatureMiddleware>();
            services.AddSingleton<FakeTimeProvider>();
            services.AddSingleton<TimeProvider>(s => s.GetRequiredService<FakeTimeProvider>());
        }

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Used through reflection")]
        public static void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseMiddleware<TestBodyPipeFeatureMiddleware>();
            app.UseHttpLogging();

            app.Map("/error", static x =>
                x.Run(static async context =>
                {
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

                    context.RequestServices.GetRequiredService<FakeTimeProvider>().Advance(TimeSpan.FromMilliseconds(ErrorRouteProcessingTimeMs));
                    throw new InvalidOperationException("Test exception");
                }));

            app.Run(static async context =>
            {
                context.RequestServices.GetRequiredService<FakeTimeProvider>().Advance(TimeSpan.FromMilliseconds(SlashRouteProcessingTimeMs));

                await context.Request.Body.DrainAsync(default);

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
            .ConfigureServices(x => x.AddSingleton<TestBodyPipeFeatureMiddleware>())
            .ConfigureServices(configure)
            .ConfigureWebHost(static builder => builder
                .UseStartup<TStartup>()
                .UseTestServer())
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
            services => services.AddHttpLoggingRedaction(configureLogging: x =>
            {
                x.MediaTypeOptions.AddText(responseContentTypeToLog);
                x.LoggingFields |= HttpLogging.HttpLoggingFields.ResponseBody;
            }),
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
                    Assert.Equal(7, state!.Count);
                }
                else
                {
                    Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
                    Assert.Equal(6, state!.Count);
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
            services => services.AddHttpLoggingRedaction(configureLogging: x =>
            {
                x.MediaTypeOptions.Clear();
                x.MediaTypeOptions.AddText("text/*");
                x.LoggingFields |= HttpLoggingFields.RequestBody;
            }),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content, null, requestContentType);
                using var response = await client.PostAsync("/", content).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                var logs = logCollector.GetSnapshot();
                Assert.Equal(shouldLog ? 3 : 2, logs.Count);
                Assert.Null(logs[0].Exception);
                Assert.Null(logs[^1].Exception);
                Assert.Equal(LogLevel.Information, logs[0].Level);
                Assert.Equal(LogLevel.Information, logs[^1].Level);
                Assert.Equal(LoggingCategory, logs[0].Category);
                Assert.Equal(LoggingCategory, logs[^1].Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state0 = logs[0].StructuredState!;
                var state2 = logs[^1].StructuredState!;

                Assert.Equal(5, state0!.Count);
                Assert.Equal(2, state2!.Count);
                Assert.DoesNotContain(state2, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.DoesNotContain(state0, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state2, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(state2, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Post.ToString());
                Assert.Single(state2, x => x.Key == HttpLoggingTagNames.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == SlashRouteProcessingTimeMs);

                if (shouldLog)
                {
                    var state1 = logs[1].StructuredState!;
                    Assert.Equal(2, state1!.Count);
                    Assert.Single(state1, x => x.Key == HttpLoggingTagNames.RequestBody && x.Value == Content);
                }
            });
    }

    [Fact]
    public async Task HttpLogging_WhenMultiSegmentRequestPipe_LogRequestBody()
    {
        await RunAsync(
            LogLevel.Information,
            services => services.AddHttpLoggingRedaction(configureLogging: x =>
            {
                x.MediaTypeOptions.AddText("text/*");
                x.LoggingFields |= HttpLogging.HttpLoggingFields.RequestBody;
            }),
            async (logCollector, client) =>
            {
                const string Content = "Whatever...";

                using var content = new StringContent(Content, null, MediaTypeNames.Text.Plain);
                using var response = await client.PostAsync("/multi-segment-pipe", content).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal(1, logCollector.Count);
                Assert.Null(logCollector.LatestRecord.Exception);
                Assert.Equal(LogLevel.Information, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(7, state!.Count);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Post.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.RequestBody && x.Value == "Test Segment");
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
            }, static x =>
            {
                x.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders;
                x.MediaTypeOptions.AddText(MediaTypeNames.Text.Plain);
            }),
            async static (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var request = new HttpRequestMessage(HttpMethod.Post, "/")
                {
                    Content = new StringContent(Content)
                };

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
                using var response = await client.SendAsync(request).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                var logRecords = logCollector.GetSnapshot();
                Assert.Equal(2, logRecords.Count);
                Assert.All(logRecords, x => Assert.Null(x.Exception));
                Assert.All(logRecords, x => Assert.Equal(LogLevel.Information, x.Level));
                Assert.All(logRecords, x => Assert.Equal(LoggingCategory, x.Category));

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var firstState = logRecords[0].StructuredState;
                var secondState = logRecords[1].StructuredState;

                Assert.Equal(6, firstState!.Count);
                Assert.DoesNotContain(firstState, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.DoesNotContain(firstState, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.DoesNotContain(firstState, x => x.Key == HttpLoggingTagNames.StatusCode);
                Assert.DoesNotContain(firstState, x => x.Key == HttpLoggingTagNames.Duration);
                // Assert.Single(firstState, x => x.Key == HttpLoggingTagNames.RequestBody && x.Value == Content);
                Assert.Single(firstState, x => x.Key == HttpLoggingTagNames.RequestHeaderPrefix + HeaderNames.Accept);
                Assert.Single(firstState, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(firstState, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(firstState, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Post.ToString());

                Assert.Equal(3, secondState!.Count);
                // Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.RequestHeaderPrefix + HeaderNames.Accept);
                Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.ResponseHeaderPrefix + HeaderNames.TransferEncoding);
                // Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.RequestBody && x.Value == Content);
                // Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.ResponseBody && x.Value == "Server: hello!Server: world!");
                // Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                // Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                // Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Post.ToString());
                Assert.Single(secondState, x => x.Key == HttpLoggingTagNames.Duration && x.Value != null);
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
                using var httpMessage = new HttpRequestMessage(HttpMethod.Get, "/");
                httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

                using var response = await client.SendAsync(httpMessage).ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                var logs = logCollector.GetSnapshot();
                Assert.Equal(2, logs.Count);

                var log0 = logs[0];
                var log1 = logs[1];
                Assert.Null(log0.Exception);
                Assert.Null(log1.Exception);
                Assert.Equal(LogLevel.Information, log0.Level);
                Assert.Equal(LogLevel.Information, log1.Level);
                Assert.Equal(LoggingCategory, log0.Category);
                Assert.Equal(LoggingCategory, log1.Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state0 = log0.StructuredState;
                var state1 = log1.StructuredState;

                Assert.Equal(6, state0!.Count);
                Assert.Equal(3, state1!.Count);
                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.RequestHeaderPrefix + HeaderNames.Accept);
                Assert.Single(state1, x => x.Key == HttpLoggingTagNames.ResponseHeaderPrefix + HeaderNames.TransferEncoding);
                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.Host && !string.IsNullOrEmpty(x.Value));
                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.Path && x.Value == TelemetryConstants.Unknown);
                Assert.Single(state1, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(state1, x => x.Key == HttpLoggingTagNames.Duration &&
                    x.Value != null &&
                    int.Parse(x.Value, CultureInfo.InvariantCulture) == SlashRouteProcessingTimeMs);

                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Get.ToString());
                Assert.DoesNotContain(state0, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state1, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.DoesNotContain(state0, x =>
                    x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix) && !x.Key.EndsWith(HeaderNames.Accept));

                Assert.DoesNotContain(state1, x =>
                    x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix) && !x.Key.EndsWith(HeaderNames.TransferEncoding));
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

                var records = logCollector.GetSnapshot();
                Assert.Equal(2, records.Count);
                Assert.Null(records[0].Exception);
                Assert.Null(records[1].Exception);
                Assert.Equal(LogLevel.Information, records[0].Level);
                Assert.Equal(LogLevel.Information, records[1].Level);
                Assert.Equal(LoggingCategory, records[0].Category);
                Assert.Equal(LoggingCategory, records[1].Category);

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var state0 = records[0].StructuredState;
                var state1 = records[1].StructuredState;

                Assert.Equal(5, state0!.Count);
                Assert.Equal(4, state1!.Count);
                Assert.Single(state1, x => x.Key == TestHttpLogEnricher.Key1 && x.Value == TestHttpLogEnricher.Value1);
                Assert.Single(state1, x => x.Key == TestHttpLogEnricher.Key2 && x.Value == TestHttpLogEnricher.Value2.ToString(CultureInfo.CurrentCulture));
                Assert.Single(state0, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Delete.ToString());
                Assert.DoesNotContain(state0, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state1, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.DoesNotContain(state0, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state1, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
            });
    }

    [Fact]
    public async Task HttpLogging_WhenMultipleEnrichersAdded_ErrorLog_CorrectlyFormatted()
    {
        await RunAsync(
            LogLevel.Information,
            static x =>
            {
                x.AddHttpLogEnricher<TestHttpLogEnricher>();
                x.AddHttpLogEnricher<CustomHttpLogEnricher>();
                x.AddHttpLoggingRedaction();
            },
            async (logCollector, client) =>
            {
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/error"));
                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);
                Assert.Equal("GET localhost/unknown", logCollector.LatestRecord.Message);
            });
    }

    [Fact]
    public async Task HttpLogging_WhenMultipleEnrichersAdded_InformationLog_CorrectlyFormatted()
    {
        await RunAsync(
            LogLevel.Information,
            static x =>
            {
                x.AddHttpLogEnricher<TestHttpLogEnricher>();
                x.AddHttpLogEnricher<CustomHttpLogEnricher>();
                x.AddHttpLoggingRedaction(opt => opt.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None);
            },
            async (logCollector, client) =>
            {
                await client.GetAsync("/api/users/123/add-task/345");
                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);
                Assert.Equal("GET localhost//api/users/123/add-task/345", logCollector.LatestRecord.Message);
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

                Assert.Equal(6, state!.Count);
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
                x.AddHttpLoggingRedaction(configureLogging: x => x.LoggingFields |= HttpLoggingFields.Request);
            },
            async static (logCollector, client) =>
            {
                using var response = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.True(response.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                var logRecords = logCollector.GetSnapshot();
                Assert.Equal(2, logRecords.Count);
                Assert.All(logRecords, x => Assert.Null(x.Exception));
                Assert.All(logRecords, x => Assert.Equal(LogLevel.Information, x.Level));
                Assert.All(logRecords, x => Assert.Equal(LoggingCategory, x.Category));

                var responseStatus = ((int)response.StatusCode).ToInvariantString();
                var firstState = logRecords[0].StructuredState;
                var secondState = logRecords[1].StructuredState;

                Assert.Equal(4, firstState!.Count);
                Assert.DoesNotContain(firstState, x => x.Key == TestHttpLogEnricher.Key1 && x.Value == TestHttpLogEnricher.Value1);
                Assert.DoesNotContain(firstState, x => x.Key == TestHttpLogEnricher.Key2 && x.Value == TestHttpLogEnricher.Value2.ToString(CultureInfo.CurrentCulture));

                Assert.Equal(8, secondState!.Count);
                Assert.Single(secondState, x => x.Key == TestHttpLogEnricher.Key1 && x.Value == TestHttpLogEnricher.Value1);
                Assert.Single(secondState, x => x.Key == TestHttpLogEnricher.Key2 && x.Value == TestHttpLogEnricher.Value2.ToString(CultureInfo.CurrentCulture));
            });
    }

    [Fact]
    public async Task HttpLogging_WhenSecondLogRequestStart_DontLogDurationAndStatus()
    {
        await RunAsync(
            LogLevel.Information,
            static x => x.AddHttpLoggingRedaction(configureLogging: x => x.LoggingFields |= HttpLoggingFields.Request),
            async static (logCollector, client) =>
            {
                using var firstResponse = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.True(firstResponse.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                using var secondResponse = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.True(secondResponse.IsSuccessStatusCode);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 4);

                var logRecords = logCollector.GetSnapshot();
                Assert.Equal(4, logRecords.Count);
                Assert.All(logRecords, x => Assert.Null(x.Exception));
                Assert.All(logRecords, x => Assert.Equal(LogLevel.Information, x.Level));
                Assert.All(logRecords, x => Assert.Equal(LoggingCategory, x.Category));

                var responseStatus = ((int)firstResponse.StatusCode).ToInvariantString();
                var firstRecord = logRecords[0].StructuredState;
                var secondRecord = logRecords[1].StructuredState;
                var thirdRecord = logRecords[2].StructuredState;
                var fourthRecord = logRecords[3].StructuredState;

                Assert.Equal(4, firstRecord!.Count);
                Assert.Equal(4, thirdRecord!.Count);
                Assert.DoesNotContain(firstRecord, x => x.Key == HttpLoggingTagNames.StatusCode);
                Assert.DoesNotContain(firstRecord, x => x.Key == HttpLoggingTagNames.Duration);
                Assert.DoesNotContain(thirdRecord, x => x.Key == HttpLoggingTagNames.StatusCode);
                Assert.DoesNotContain(thirdRecord, x => x.Key == HttpLoggingTagNames.Duration);

                Assert.Equal(6, secondRecord!.Count);
                Assert.Equal(6, fourthRecord!.Count);
                Assert.Single(secondRecord, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(secondRecord, x => x.Key == HttpLoggingTagNames.Duration && x.Value != null);
                Assert.Single(fourthRecord, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == responseStatus);
                Assert.Single(fourthRecord, x => x.Key == HttpLoggingTagNames.Duration && x.Value != null);
            });
    }

    [Theory]
    [InlineData("/error", "0")]
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
                Assert.Equal(LogLevel.Error, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);
                Assert.NotNull(logCollector.LatestRecord.Exception);
                Assert.Same(ex, logCollector.LatestRecord.Exception);

                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(6, state!.Count);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
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
            static services => services.AddHttpLoggingRedaction(configureLogging: static x =>
            {
                x.MediaTypeOptions.AddText(MediaTypeNames.Text.Plain);
                x.LoggingFields |= HttpLoggingFields.RequestBody;
            }),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content);
                var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.PutAsync("/error?body=true", content));

                var originalException = ex.InnerException?.InnerException;
                Assert.NotNull(originalException);
                Assert.Equal("Test exception", originalException!.Message);

                Assert.Equal(1, logCollector.Count);
                Assert.Equal(LogLevel.Error, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);
                Assert.NotNull(logCollector.LatestRecord.Exception);
                Assert.Same(originalException, logCollector.LatestRecord.Exception);

                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(8, state!.Count);
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
            static services => services.AddHttpLoggingRedaction(configureLogging: static x => x.LoggingFields &= ~HttpLoggingFields.ResponseBody),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content);
                var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.PutAsync("/error?body=true", content));

                var originalException = ex.InnerException?.InnerException;
                Assert.NotNull(originalException);
                Assert.Equal("Test exception", originalException!.Message);

                Assert.Equal(1, logCollector.Count);
                Assert.Equal(LogLevel.Error, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);
                Assert.NotNull(logCollector.LatestRecord.Exception);
                Assert.Same(originalException, logCollector.LatestRecord.Exception);

                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(6, state!.Count);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Put.ToString());
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
            });
    }

    [Fact]
    public async Task HttpLogging_WhenRequestBodyReadTimeout_LogException()
    {
        await RunAsync<TestStartup>(
            LogLevel.Information,
            static services => services.AddHttpLoggingRedaction(configureLogging: static x =>
            {
                x.MediaTypeOptions.AddText(MediaTypeNames.Text.Plain);
                x.LoggingFields |= HttpLoggingFields.RequestBody;
                x.RequestBodyLogLimit = int.MaxValue;
            }),
            async (logCollector, client, serviceProvider) =>
            {
                using var stream = new InfiniteStream('A');
                using var content = new StreamContent(stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Plain);

                using var cts = new CancellationTokenSource();
                var pipeMiddleware = serviceProvider.GetRequiredService<TestBodyPipeFeatureMiddleware>();
                pipeMiddleware.RequestBodyInfinitePipeFeatureCallback = cts.Cancel;

                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.PutAsync("/infinite-pipe", content, cts.Token));
                Assert.NotNull(ex);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout);

                Assert.Equal(1, logCollector.Count);
                Assert.Equal(LogLevel.Error, logCollector.LatestRecord.Level);
                Assert.Equal(LoggingCategory, logCollector.LatestRecord.Category);
                Assert.NotNull(logCollector.LatestRecord.Exception);
                Assert.IsType<OperationCanceledException>(logCollector.LatestRecord.Exception);

                var state = logCollector.LatestRecord.StructuredState;

                Assert.Equal(6, state!.Count);
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.RequestHeaderPrefix));
                Assert.DoesNotContain(state, x => x.Key.StartsWith(HttpLoggingTagNames.ResponseHeaderPrefix));
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Put.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == "0");
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
            });
    }

    [Fact]
    public async Task HttpLogging_WhenRequestBodyReadError_LogException()
    {
        await RunAsync(
            LogLevel.Information,
            static services => services.AddHttpLoggingRedaction(configureLogging: static x =>
            {
                x.MediaTypeOptions.AddText(MediaTypeNames.Text.Plain);
                x.LoggingFields |= HttpLoggingFields.RequestBody;
            }),
            async (logCollector, client) =>
            {
                const string Content = "Client: hello!";

                using var content = new StringContent(Content);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Plain);

                using var response = await client.PutAsync("/err-pipe", content);

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                var records = logCollector.GetSnapshot();
                Assert.Equal(2, records.Count);
                var firstRecord = records[0];
                var secondRecord = records[1];

                Assert.Equal(LogLevel.Error, firstRecord.Level);
                Assert.All(records, x => Assert.Equal(LoggingCategory, x.Category));
                Assert.NotNull(firstRecord.Exception);
                // Assert.Equal(Log.ReadingRequestBodyError, firstRecord.Message);
                Assert.Equal(RequestBodyErrorPipeFeature.ErrorMessage, firstRecord.Exception!.Message);

                var state = secondRecord.StructuredState;

                Assert.Equal(6, state!.Count);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Put.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == ((int)response.StatusCode).ToInvariantString());
            });
    }

    [Fact]
    public async Task HttpLogging_WhenResponseBodyReadError_LogException()
    {
        const string ExceptionMessage = "Exception on response body intercepting";
        /*
        static ReadOnlyMemory<byte> SyntheticInterceptingDataGetter(ResponseInterceptingStream _)
            => throw new InvalidOperationException(ExceptionMessage);
        */
        await RunAsync<TestStartup>(
            LogLevel.Information,
            static services => services.AddHttpLoggingRedaction(configureLogging: static x =>
            {
                x.MediaTypeOptions.AddText(MediaTypeNames.Text.Plain);
                x.LoggingFields |= HttpLoggingFields.ResponseBody;
            }),
            async (logCollector, client, _) =>
            {
                using var response = await client.GetAsync("/");

                await WaitForLogRecordsAsync(logCollector, _defaultLogTimeout, expectedRecords: 2);

                var records = logCollector.GetSnapshot();
                Assert.Equal(2, records.Count);
                var firstRecord = records[0];
                var secondRecord = records[1];

                Assert.Equal(LogLevel.Error, firstRecord.Level);
                Assert.All(records, x => Assert.Equal(LoggingCategory, x.Category));
                Assert.NotNull(firstRecord.Exception);
                // Assert.Equal(Log.ReadingResponseBodyError, firstRecord.Message);
                Assert.Equal(ExceptionMessage, firstRecord.Exception!.Message);

                var state = secondRecord.StructuredState;

                Assert.Equal(6, state!.Count);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.RequestBody);
                Assert.DoesNotContain(state, x => x.Key == HttpLoggingTagNames.ResponseBody);
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.Method && x.Value == HttpMethod.Get.ToString());
                Assert.Single(state, x => x.Key == HttpLoggingTagNames.StatusCode && x.Value == ((int)response.StatusCode).ToInvariantString());
            }
            /*, configureMiddleware: static x => x.GetResponseBodyInterceptedData = SyntheticInterceptingDataGetter*/);
    }

    [Fact]
    public async Task HttpLogging_WhenLogLevelWarning_NoLogHttp_ButWarning()
    {
        await RunAsync(
            LogLevel.Warning,
            static _ => { },
            static (logCollector, _) =>
            {
                Assert.Equal(1, logCollector.Count);

                var latestRecord = logCollector.LatestRecord;
                Assert.Equal(LoggingCategory, latestRecord.Category);
                Assert.Equal(LogLevel.Warning, latestRecord.Level);
                Assert.StartsWith("HttpLogging middleware is injected into application pipeline, but LogLevel", latestRecord.Message);
                return Task.CompletedTask;
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
                    Assert.Equal(2, logCollector.Count);
                    Assert.Equal(5, logCollector.GetSnapshot()[0].StructuredState!.Count);
                    Assert.Equal(2, logCollector.GetSnapshot()[1].StructuredState!.Count);
                }
            });
    }
}
#endif
