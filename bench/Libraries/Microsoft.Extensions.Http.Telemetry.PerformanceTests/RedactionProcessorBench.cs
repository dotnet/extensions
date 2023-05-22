// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Bench;

#pragma warning disable S1075 // URIs should not be hardcoded
#pragma warning disable SA1203 // Constants should appear before fields

[GcServer(true)]
[MinColumn]
[MaxColumn]
[MemoryDiagnoser]
#pragma warning disable CA1063 // Implement IDisposable Correctly
public class RedactionProcessorBench : IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
{
    private const string ShortPath = "http://test.com/api/routes/routeId123/chats/chatId123/routeId123/chats/chatId123";
    private const string ShortRoute = "/api/routes/{routeId}/chats/{chatId}/{routeId}/chats/{chatId}";
    private HttpClientRedactionProcessor? _shortRedactionProcessor;
    private Activity? _shortAcivity;
    private HttpRequestMessage? _shortMessage;

    private const string LongPath
        = "http://test.com/api/something/something/something/something/something/FJWIEFNJIWEFJI/FJWIEFNJIWEFJI/something/FJWIEFNJIWEFJI/FJWIEFNJIWEFJI/routes/route123/users/user232/chats/chatr213";
    private const string LongRoute = "/api/something/something/something/something/something/{blabla}/{blabla}/something/{blabla}/{blabla}/routes/{routeId}/users/{userId}/chats/{chatId}";
    private HttpClientRedactionProcessor? _longRedactionProcessor;
    private Activity? _longAcivity;
    private HttpRequestMessage? _longMessage;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddOutgoingRequestContext()
            .BuildServiceProvider();

        IRedactorProvider redactorProvider = new RP();
        IHttpPathRedactor httpPathRedactor = builder.GetService<IHttpPathRedactor>()!;
        var requestMetadataContext = builder.GetService<IOutgoingRequestContext>()!;

        var options = new HttpClientTracingOptions();

        options.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        options.RouteParameterDataClasses.Add("chatId", SimpleClassifications.PrivateData);
        options.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);

        _shortRedactionProcessor = new HttpClientRedactionProcessor(
            Microsoft.Extensions.Options.Options.Create(options),
            httpPathRedactor,
            requestMetadataContext);

        _shortAcivity = new Activity("short_activity");
        _shortMessage = new()
        {
            RequestUri = new Uri(ShortPath)
        };
        _shortMessage.SetRequestMetadata(new RequestMetadata { RequestRoute = ShortRoute });
        _shortAcivity.SetCustomProperty(Constants.CustomPropertyHttpRequestMessage, _shortMessage);

        var longOptions = new HttpClientTracingOptions();

        longOptions.RouteParameterDataClasses.Add("routeId", SimpleClassifications.PrivateData);
        longOptions.RouteParameterDataClasses.Add("chatId", SimpleClassifications.PrivateData);
        longOptions.RouteParameterDataClasses.Add("userId", SimpleClassifications.PrivateData);
        longOptions.RouteParameterDataClasses.Add("blabla", SimpleClassifications.PrivateData);

        _longRedactionProcessor = new HttpClientRedactionProcessor(
            Microsoft.Extensions.Options.Options.Create(longOptions),
            httpPathRedactor,
            requestMetadataContext);

        _longAcivity = new Activity("long_activity");
        _longMessage = new()
        {
            RequestUri = new Uri(LongPath)
        };
        _longMessage.SetRequestMetadata(new RequestMetadata { RequestRoute = LongRoute });
        _longAcivity.SetCustomProperty(Constants.CustomPropertyHttpRequestMessage, _longMessage);
    }

    [Benchmark]
    public int Short_Uri_Processing()
    {
        _shortRedactionProcessor!.Process(_shortAcivity!, _shortMessage!);

        return 0;
    }

    [Benchmark]
    public int Long_Uri_Processing()
    {
        _longRedactionProcessor!.Process(_longAcivity!, _longMessage!);

        return 0;
    }

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        _shortAcivity?.Dispose();
        _shortMessage?.Dispose();

        _longAcivity?.Dispose();
        _longMessage?.Dispose();
    }

    private sealed class RP : IRedactorProvider
    {
        private static readonly Redactor _r = new MockRedactor();

        public Redactor GetRedactor(DataClassification classification) => _r;
    }

    internal sealed class MockRedactor : Redactor
    {
        private const string Template = "[REDACTED]";

        public override int GetRedactedLength(ReadOnlySpan<char> source)
        {
            return Template.Length;
        }

        public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            Template.AsSpan().CopyTo(destination);
            return Template.Length;
        }
    }
}
