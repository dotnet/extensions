// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals;
public class HttpResiliencePipelineBuilderTest
{
    [Fact]
    public void Ctor_Ok()
    {
        var services = new ServiceCollection();
        var builder = services.AddResiliencePipeline<HttpResponseMessage>("test");

        var httpBuilder = new HttpResiliencePipelineBuilder(builder);

        Assert.Equal(services, httpBuilder.Services);
        Assert.Equal("test", httpBuilder.PipelineName);
    }
}
