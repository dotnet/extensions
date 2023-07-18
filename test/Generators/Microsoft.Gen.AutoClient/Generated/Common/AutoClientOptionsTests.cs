// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.AutoClient;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "IDisposable inside mock setups")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1067:Expressions should not be too complex", Justification = "Mock conditions")]
public class AutoClientOptionsTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);

    public AutoClientOptionsTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });
    }

    [Fact]
    public async Task JsonBodyUsesJsonSerializerOptions()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(_ => _factoryMock.Object)
            .AddRestApiClientOptionsApi(options =>
            {
                options.JsonSerializerOptions = new JsonSerializerOptions
                {
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                };
            })
            .BuildServiceProvider();
        var sut = provider.GetRequiredService<IRestApiClientOptionsApi>();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Post &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/dict" &&
                message.Content!.Headers.ContentType!.ToString() == "application/json; charset=utf-8" &&
                message.Content!.ReadAsStringAsync().Result == @"{""myProperty"":""MyValue""}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await sut.PostDictionary(new Dictionary<string, string>
        {
            ["MyProperty"] = "MyValue"
        });

        Assert.Equal("Success!", response);
    }

    [Fact]
    public void GivenNullJsonSerializerOptions_Throws()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(_ => _factoryMock.Object)
            .AddRestApiClientOptionsApi(options => options.JsonSerializerOptions = null!)
            .BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(provider.GetRequiredService<IRestApiClientOptionsApi>);
    }
}
