// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly;
using Polly.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

public class HttpResilienceMetricsEnricherTests
{
    [Fact]
    public void IncompatibleType_NotTagsAdded()
    {
        var tags = new List<KeyValuePair<string, object?>>();

        var context = new EnrichmentContext<string, string>(default, tags);
        var enricher = new HttpResilienceMetricsEnricher();

        enricher.Enrich(in context);

        tags.Should().BeEmpty();
    }

    [Fact]
    public void NoResponse_NoTagsAdded()
    {
        var tags = new List<KeyValuePair<string, object?>>();

        var context = new EnrichmentContext<HttpResponseMessage, string>(default, tags);
        var enricher = new HttpResilienceMetricsEnricher();

        enricher.Enrich(in context);

        tags.Should().BeEmpty();
    }

    [Fact]
    public void Response_ErrorTypeTagAdded()
    {
        var tags = new List<KeyValuePair<string, object?>>();
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        var args = new TelemetryEventArguments<HttpResponseMessage, string>(default!, default!, default!, default!, Outcome.FromResult(response));
        var context = new EnrichmentContext<HttpResponseMessage, string>(args, tags);
        var enricher = new HttpResilienceMetricsEnricher();

        enricher.Enrich(in context);

        tags.Should().HaveCount(1);
        tags[0].Key.Should().Be("error.type");
        tags[0].Value.Should().Be("500");
    }
}
