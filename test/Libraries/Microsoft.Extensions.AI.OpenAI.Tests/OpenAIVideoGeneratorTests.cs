// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Videos;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIVideoGeneratorTests
{
    [Fact]
    public void AsIVideoGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("videoClient", () => ((VideoClient)null!).AsIVideoGenerator());
    }

    [Fact]
    public void AsIVideoGenerator_OpenAIClient_ProducesExpectedMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        string model = "sora";

        var client = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IVideoGenerator videoGenerator = client.GetVideoClient().AsIVideoGenerator(model);
        var metadata = videoGenerator.GetService<VideoGeneratorMetadata>();
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal(model, metadata?.DefaultModelId);
    }

    [Fact]
    public void GetService_ReturnsExpectedServices()
    {
        var client = new OpenAIClient(new ApiKeyCredential("key"));
        IVideoGenerator videoGenerator = client.GetVideoClient().AsIVideoGenerator("sora");

        Assert.Same(videoGenerator, videoGenerator.GetService<IVideoGenerator>());
        Assert.Same(videoGenerator, videoGenerator.GetService<object>());
        Assert.NotNull(videoGenerator.GetService<VideoGeneratorMetadata>());
        Assert.NotNull(videoGenerator.GetService<VideoClient>());
    }

    [Fact]
    public async Task GenerateAsync_CreateWithDuration_EmitsSecondsAsString()
    {
        string? capturedBody = null;
        using var handler = new RoutingHandler(request =>
        {
            capturedBody = request.Content!.ReadAsStringAsync().Result;
            return CreateVideoResponse("vid_123", "queued");
        });
        using HttpClient httpClient = new(handler);
        using var generator = CreateGenerator(httpClient);

        await generator.GenerateAsync(
            new VideoGenerationRequest { Prompt = "test" },
            new VideoGenerationOptions { Duration = TimeSpan.FromSeconds(8) });

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        JsonElement root = doc.RootElement;

        // The API requires seconds as a string enum, not an integer
        Assert.Equal(JsonValueKind.String, root.GetProperty("seconds").ValueKind);
        Assert.Equal("8", root.GetProperty("seconds").GetString());
    }

    [Fact]
    public async Task GenerateAsync_ExtendWithDuration_EmitsSecondsAsString()
    {
        string? capturedBody = null;
        using var handler = new RoutingHandler(request =>
        {
            capturedBody = request.Content!.ReadAsStringAsync().Result;
            return CreateVideoResponse("vid_456", "queued");
        });
        using HttpClient httpClient = new(handler);
        using var generator = CreateGenerator(httpClient);

        await generator.GenerateAsync(
            new VideoGenerationRequest
            {
                Prompt = "continue the scene",
                OperationKind = VideoOperationKind.Extend,
                SourceVideoId = "vid_original",
            },
            new VideoGenerationOptions { Duration = TimeSpan.FromSeconds(12) });

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        JsonElement root = doc.RootElement;

        Assert.Equal(JsonValueKind.String, root.GetProperty("seconds").ValueKind);
        Assert.Equal("12", root.GetProperty("seconds").GetString());
    }

    [Fact]
    public async Task GenerateAsync_ImageReferenceData_SentAsJsonDataUri()
    {
        string? capturedBody = null;
        string? capturedContentType = null;
        using var handler = new RoutingHandler(request =>
        {
            capturedContentType = request.Content!.Headers.ContentType?.ToString();
            capturedBody = request.Content!.ReadAsStringAsync().Result;
            return CreateVideoResponse("vid_789", "queued");
        });
        using HttpClient httpClient = new(handler);
        using var generator = CreateGenerator(httpClient);

        byte[] imageBytes = [0x89, 0x50, 0x4E, 0x47]; // PNG magic bytes
        await generator.GenerateAsync(
            new VideoGenerationRequest
            {
                Prompt = "animate this image",
                OriginalMedia = [new DataContent(imageBytes, "image/png")],
            });

        // Should be JSON, not multipart
        Assert.NotNull(capturedContentType);
        Assert.Contains("application/json", capturedContentType);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        JsonElement root = doc.RootElement;

        // input_reference should be a JSON object with image_url as a data URI
        Assert.True(root.TryGetProperty("input_reference", out JsonElement inputRef));
        Assert.Equal(JsonValueKind.Object, inputRef.ValueKind);
        string imageUrl = inputRef.GetProperty("image_url").GetString()!;
        Assert.StartsWith("data:image/png;base64,", imageUrl);
    }

    [Fact]
    public async Task UploadVideoCharacterAsync_ApiError_ThrowsWithMessage()
    {
        using var handler = new RoutingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"error": {"message": "Video too long"}}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        using HttpClient httpClient = new(handler);
        using var generator = CreateGenerator(httpClient);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => generator.UploadVideoCharacterAsync(
                "TestChar",
                new DataContent(new byte[100], "video/mp4")));

        Assert.Contains("Video too long", ex.Message);
    }

    [Fact]
    public async Task UploadVideoCharacterAsync_MissingId_ThrowsWithResponseBody()
    {
        using var handler = new RoutingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"unexpected": "response"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        using HttpClient httpClient = new(handler);
        using var generator = CreateGenerator(httpClient);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => generator.UploadVideoCharacterAsync(
                "TestChar",
                new DataContent(new byte[100], "video/mp4")));

        Assert.Contains("did not contain an 'id' property", ex.Message);
        Assert.Contains("unexpected", ex.Message);
    }

    [Fact]
    public async Task UploadVideoCharacterAsync_Success_ReturnsCharacterId()
    {
        using var handler = new RoutingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id": "char_abc123", "created_at": 0, "name": "TestChar"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        using HttpClient httpClient = new(handler);
        using var generator = CreateGenerator(httpClient);

        string id = await generator.UploadVideoCharacterAsync(
            "TestChar",
            new DataContent(new byte[100], "video/mp4"));

        Assert.Equal("char_abc123", id);
    }

    private static IVideoGenerator CreateGenerator(HttpClient httpClient) =>
        new OpenAIClient(
            new ApiKeyCredential("test-key"),
            new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) })
        .GetVideoClient()
        .AsIVideoGenerator("sora-2");

    private static HttpResponseMessage CreateVideoResponse(string id, string status)
    {
        string json = $$"""{"id": "{{id}}", "status": "{{status}}"}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
