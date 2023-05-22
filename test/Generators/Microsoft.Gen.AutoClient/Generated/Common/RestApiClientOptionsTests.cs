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
using Moq;
using Moq.Protected;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "IDisposable inside mock setups")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1067:Expressions should not be too complex", Justification = "Mock conditions")]
public class RestApiClientOptionsTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);

    private readonly IRestApiClientOptionsApi _sut;

    public RestApiClientOptionsTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });

        var services = new ServiceCollection();
        services.AddSingleton(_ => _factoryMock.Object);
        services.AddRestApiClientOptionsApi(options =>
        {
            options.JsonSerializerOptions = new JsonSerializerOptions
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            };
        });
        var provider = services.BuildServiceProvider();

        _sut = provider.GetRequiredService<IRestApiClientOptionsApi>();
    }

    [Fact]
    public async Task JsonBodyUsesJsonSerializerOptions()
    {
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

        var response = await _sut.PostDictionary(new Dictionary<string, string>
        {
            ["MyProperty"] = "MyValue"
        });

        Assert.Equal("Success!", response);
    }
}
