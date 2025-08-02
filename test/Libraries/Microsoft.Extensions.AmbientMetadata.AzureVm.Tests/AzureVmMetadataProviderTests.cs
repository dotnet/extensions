// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AmbientMetadata.Internal;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class AzureVmMetadataProviderTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;

    public AzureVmMetadataProviderTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public async Task GetMetadataAsync_WithValidHttpResponse_ReturnsExpectedMetadata()
    {
        // Arrange
        var expectedMetadata = new AzureVmMetadata
        {
            Location = "West US",
            Name = "my-vm",
            Offer = "UbuntuServer",
            OsType = "Linux",
            PlatformFaultDomain = "1",
            PlatformUpdateDomain = "2",
            Publisher = "Canonical",
            Sku = "18.04-LTS",
            SubscriptionId = "12345678-1234-5678-abcd-1234567890ab",
            Version = "18.04.202110080",
            VmId = "12345678-1234-5678-abcd-1234567890ab",
            VmScaleSetName = null,
            VmSize = "Standard_D2_v2"
        };
        using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedMetadata, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage)
            .Verifiable();

        using var magicHttpClient = new HttpClient(_handlerMock.Object);
        var metadataProvider = new AzureVmMetadataProvider(magicHttpClient);

        // Act
        AzureVmMetadata result = await metadataProvider.GetMetadataAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedMetadata);
    }

    [Fact]
    public async Task GetMetadataAsync_WithEmptyHttpResponse_ReturnsEmptyMetadata()
    {
        // Arrange
        using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        };
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage)
            .Verifiable();
        using var magicHttpClient = new HttpClient(_handlerMock.Object);
        var metadataProvider = new AzureVmMetadataProvider(magicHttpClient);

        // Act
        AzureVmMetadata result = await metadataProvider.GetMetadataAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new AzureVmMetadata());
    }

    [Fact]
    public async Task GetMetadataAsync_WithInvalidHttpResponse_ReturnsEmptyMetadata()
    {
        // Arrange
        using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage)
            .Verifiable();

        using var magicHttpClient = new HttpClient(_handlerMock.Object);
        var metadataProvider = new AzureVmMetadataProvider(magicHttpClient);

        // Act
        AzureVmMetadata result = await metadataProvider.GetMetadataAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new AzureVmMetadata());
    }

    [Fact]
    public async Task GetMetadataAsync_WithExceptionDuringHttpRequest_ReturnsEmptyMetadata()
    {
        // Arrange

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        using var magicHttpClient = new HttpClient(_handlerMock.Object);
        var metadataProvider = new AzureVmMetadataProvider(magicHttpClient);

        // Act
        AzureVmMetadata result = await metadataProvider.GetMetadataAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new AzureVmMetadata());
    }
}
