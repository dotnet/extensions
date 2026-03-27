// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Multi-Provider Video Generation POC
//
// Demonstrates using IVideoGenerator with multiple providers through a unified CLI.
// Supports: OpenAI (Sora), Google Veo, Runway, and Luma AI.
//
// Usage:
//   dotnet run -- generate --provider openai "A cat playing piano"
//   dotnet run -- generate --provider veo "A cinematic drone shot" --audio --aspect-ratio 16:9
//   dotnet run -- generate --provider runway "A bunny hopping" --seed 42
//   dotnet run -- generate --provider luma "A tiger in snow" --aspect-ratio 16:9
//   dotnet run -- image-to-video --provider openai "The scene comes alive" --image photo.jpg
//   dotnet run -- edit --provider openai "Change sky to sunset" --video <video-id>
//   dotnet run -- extend --provider openai "Continue the scene" --video <video-id>
//
// Environment variables (set the ones for the providers you plan to use):
//   OPENAI_API_KEY  — OpenAI API key
//   GOOGLE_API_KEY  — Google Gemini API key
//   RUNWAY_API_KEY  — Runway API key
//   LUMA_API_KEY    — Luma AI API key

using System.CommandLine;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

// ── Shared options ─────────────────────────────────────────────────────────
var providerOption = new Option<VideoProvider>("--provider", () => VideoProvider.OpenAI, "Video generation provider.");
var modelOption = new Option<string?>("--model", "Model ID (provider-specific). Uses provider default if omitted.");
var outputOption = new Option<string?>("--output", "Output file path (.mp4).");
var durationOption = new Option<int?>("--duration", "Duration in seconds.");
var aspectRatioOption = new Option<string?>("--aspect-ratio", "Aspect ratio (e.g. 16:9, 9:16, 1:1).");
var seedOption = new Option<int?>("--seed", "Seed for reproducible generation.");
var audioOption = new Option<bool>("--audio", () => false, "Generate audio (Veo 3+ only).");
var negativePromptOption = new Option<string?>("--negative-prompt", "What to avoid in the generated video.");
var widthOption = new Option<int?>("--width", "Video width in pixels.");
var heightOption = new Option<int?>("--height", "Video height in pixels.");
var formatOption = new Option<string>("--format", () => "data", "Response format: data or uri.");

// ── generate (text-to-video) ───────────────────────────────────────────────
var generatePromptArg = new Argument<string>("prompt", "Text prompt describing the video.");
var generateCommand = new Command("generate", "Generate a video from a text prompt.")
{
    generatePromptArg, providerOption, modelOption, outputOption,
    durationOption, aspectRatioOption, seedOption, audioOption,
    negativePromptOption, widthOption, heightOption, formatOption,
};

generateCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(generatePromptArg);
    var provider = context.ParseResult.GetValueForOption(providerOption);
    string? model = context.ParseResult.GetValueForOption(modelOption);
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    var opts = ParseSharedOptions(context, model);

    using var generator = CreateGenerator(provider, model);
    var request = new VideoGenerationRequest(prompt);
    ApplyNegativePrompt(request, context.ParseResult.GetValueForOption(negativePromptOption));

    var operation = await generator.GenerateAsync(request, opts);
    await CompleteAndSaveAsync(operation, opts, outputPath);
});

// ── image-to-video ─────────────────────────────────────────────────────────
var i2vPromptArg = new Argument<string>("prompt", "Text prompt.");
var imageOption = new Option<string>("--image", "Input image file (path or URL).") { IsRequired = true };

var i2vCommand = new Command("image-to-video", "Generate video from an image + text prompt.")
{
    i2vPromptArg, imageOption, providerOption, modelOption, outputOption,
    durationOption, aspectRatioOption, seedOption, audioOption,
    negativePromptOption, widthOption, heightOption, formatOption,
};

i2vCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(i2vPromptArg);
    string imagePath = context.ParseResult.GetValueForOption(imageOption)!;
    var provider = context.ParseResult.GetValueForOption(providerOption);
    string? model = context.ParseResult.GetValueForOption(modelOption);
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    var opts = ParseSharedOptions(context, model);

    List<AIContent> media;
    if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
    {
        media = [new UriContent(new Uri(imagePath), "image/jpeg")];
    }
    else
    {
        media = [await DataContent.LoadFromAsync(imagePath)];
    }

    using var generator = CreateGenerator(provider, model);
    var request = new VideoGenerationRequest(prompt, media);
    ApplyNegativePrompt(request, context.ParseResult.GetValueForOption(negativePromptOption));

    var operation = await generator.GenerateAsync(request, opts);
    await CompleteAndSaveAsync(operation, opts, outputPath);
});

// ── edit ───────────────────────────────────────────────────────────────────
var editPromptArg = new Argument<string>("prompt", "Prompt describing the edit.");
var editVideoOption = new Option<string>("--video", "Video ID to edit.") { IsRequired = true };
var editInputOption = new Option<string?>("--input", "Video file to upload for editing (if no --video ID).");

var editCommand = new Command("edit", "Edit an existing video.")
{
    editPromptArg, editVideoOption, editInputOption, providerOption,
    modelOption, outputOption, formatOption,
};

editCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(editPromptArg);
    string videoId = context.ParseResult.GetValueForOption(editVideoOption)!;
    string? inputPath = context.ParseResult.GetValueForOption(editInputOption);
    var provider = context.ParseResult.GetValueForOption(providerOption);
    string? model = context.ParseResult.GetValueForOption(modelOption);
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(provider, model);
    var opts = new VideoGenerationOptions
    {
        ModelId = model,
        ResponseFormat = ParseFormat(format),
    };

    List<AIContent>? media = null;
    if (inputPath is not null)
    {
        media = [await DataContent.LoadFromAsync(inputPath)];
    }

    var request = new VideoGenerationRequest(prompt, media)
    {
        OperationKind = VideoOperationKind.Edit,
        SourceVideoId = videoId,
    };

    var operation = await generator.GenerateAsync(request, opts);
    await CompleteAndSaveAsync(operation, opts, outputPath);
});

// ── extend ─────────────────────────────────────────────────────────────────
var extendPromptArg = new Argument<string>("prompt", "Prompt for extending the video.");
var extendVideoOption = new Option<string>("--video", "Video ID to extend.") { IsRequired = true };

var extendCommand = new Command("extend", "Extend a completed video.")
{
    extendPromptArg, extendVideoOption, providerOption,
    modelOption, outputOption, durationOption, formatOption,
};

extendCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(extendPromptArg);
    string videoId = context.ParseResult.GetValueForOption(extendVideoOption)!;
    var provider = context.ParseResult.GetValueForOption(providerOption);
    string? model = context.ParseResult.GetValueForOption(modelOption);
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    int? duration = context.ParseResult.GetValueForOption(durationOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(provider, model);
    var opts = new VideoGenerationOptions
    {
        ModelId = model,
        ResponseFormat = ParseFormat(format),
    };

    if (duration.HasValue)
    {
        opts.Duration = TimeSpan.FromSeconds(duration.Value);
    }

    var request = new VideoGenerationRequest(prompt)
    {
        OperationKind = VideoOperationKind.Extend,
        SourceVideoId = videoId,
    };

    var operation = await generator.GenerateAsync(request, opts);
    await CompleteAndSaveAsync(operation, opts, outputPath);
});

// ── Root ───────────────────────────────────────────────────────────────────
var rootCommand = new RootCommand("Multi-provider video generation POC using MEAI IVideoGenerator.\nSupports: OpenAI (Sora), Google Veo, Runway, Luma AI.")
{
    generateCommand,
    i2vCommand,
    editCommand,
    extendCommand,
};

return await rootCommand.InvokeAsync(args);

// ═══════════════════════════════════════════════════════════════════════════
// Provider factory
// ═══════════════════════════════════════════════════════════════════════════
static IVideoGenerator CreateGenerator(VideoProvider provider, string? model) => provider switch
{
    VideoProvider.OpenAI => CreateOpenAI(model),
    VideoProvider.Veo => CreateGoogleVeo(model),
    VideoProvider.Runway => CreateRunway(model),
    VideoProvider.Luma => CreateLuma(model),
    _ => throw new ArgumentException($"Unknown provider: {provider}"),
};

static IVideoGenerator CreateOpenAI(string? model)
{
    string apiKey = RequireEnvVar("OPENAI_API_KEY", "https://platform.openai.com/api-keys");
    var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
    return new OpenAIClient(apiKey)
        .GetVideoClient()
        .AsIVideoGenerator(model ?? "sora-2")
        .AsBuilder()
        .UseLogging(loggerFactory)
        .UseOpenTelemetry(loggerFactory)
        .Build();
}

static IVideoGenerator CreateGoogleVeo(string? model)
{
    string apiKey = RequireEnvVar("GOOGLE_API_KEY", "https://aistudio.google.com/apikey");
    return new GoogleVeoVideoGenerator(apiKey, model ?? "veo-3.1-generate-preview");
}

static IVideoGenerator CreateRunway(string? model)
{
    string apiKey = RequireEnvVar("RUNWAY_API_KEY", "https://dev.runwayml.com/");
    return new RunwayVideoGenerator(apiKey, model ?? "gen4_turbo");
}

static IVideoGenerator CreateLuma(string? model)
{
    string apiKey = RequireEnvVar("LUMA_API_KEY", "https://lumalabs.ai/dream-machine/api/keys");
    return new LumaVideoGenerator(apiKey, model ?? "ray-2");
}

static string RequireEnvVar(string name, string url)
{
    string? value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrEmpty(value))
    {
        Console.Error.WriteLine($"Error: Set the {name} environment variable.");
        Console.Error.WriteLine($"Get a key at {url}");
        Environment.Exit(1);
    }

    return value!;
}

// ═══════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════
VideoGenerationOptions ParseSharedOptions(System.CommandLine.Invocation.InvocationContext context, string? model)
{
    int? duration = context.ParseResult.GetValueForOption(durationOption);
    string? aspectRatio = context.ParseResult.GetValueForOption(aspectRatioOption);
    int? seed = context.ParseResult.GetValueForOption(seedOption);
    bool audio = context.ParseResult.GetValueForOption(audioOption);
    int? width = context.ParseResult.GetValueForOption(widthOption);
    int? height = context.ParseResult.GetValueForOption(heightOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    var opts = new VideoGenerationOptions
    {
        ModelId = model,
        ResponseFormat = ParseFormat(format),
    };

    if (duration.HasValue)
    {
        opts.Duration = TimeSpan.FromSeconds(duration.Value);
    }

    if (aspectRatio is not null)
    {
        opts.AspectRatio = aspectRatio;
    }

    if (seed.HasValue)
    {
        opts.Seed = seed.Value;
    }

    if (audio)
    {
        opts.GenerateAudio = true;
    }

    if (width.HasValue && height.HasValue)
    {
        opts.VideoSize = new Size(width.Value, height.Value);
    }

    return opts;
}

static void ApplyNegativePrompt(VideoGenerationRequest request, string? negativePrompt)
{
    if (negativePrompt is not null)
    {
        request.NegativePrompt = negativePrompt;
    }
}

static VideoGenerationResponseFormat ParseFormat(string format)
    => string.Equals(format, "uri", StringComparison.OrdinalIgnoreCase)
        ? VideoGenerationResponseFormat.Uri
        : VideoGenerationResponseFormat.Data;

static async Task CompleteAndSaveAsync(VideoGenerationOperation operation, VideoGenerationOptions options, string? outputPath)
{
    Console.WriteLine($"OPERATION_ID: {operation.OperationId}");
    Console.WriteLine($"  Provider: {operation.GetType().Name}");
    Console.WriteLine($"  Status: {operation.Status}");

    var sw = System.Diagnostics.Stopwatch.StartNew();
    await operation.WaitForCompletionAsync(
        new Progress<VideoGenerationProgress>(p =>
            Console.WriteLine($"  Progress: {p.Status}{(p.PercentComplete.HasValue ? $" ({p.PercentComplete}%)" : string.Empty)}")));

    sw.Stop();
    Console.WriteLine($"  Completed in {sw.Elapsed.TotalSeconds:F1}s");

    var contents = await operation.GetContentsAsync(options);
    Console.WriteLine($"  {contents.Count} content item(s)");

    for (int i = 0; i < contents.Count; i++)
    {
        string savePath = outputPath is not null && contents.Count > 1
            ? Path.Combine(
                Path.GetDirectoryName(outputPath) ?? ".",
                $"{Path.GetFileNameWithoutExtension(outputPath)}_{i}{Path.GetExtension(outputPath)}")
            : outputPath!;

        switch (contents[i])
        {
            case DataContent dc when outputPath is not null:
                Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? ".");
                await dc.SaveToAsync(savePath);
                Console.WriteLine($"  [{i}] Saved: {savePath} ({dc.Data.Length} bytes, {dc.MediaType})");
                break;
            case DataContent dc:
                Console.WriteLine($"  [{i}] DataContent: {dc.Data.Length} bytes ({dc.MediaType})");
                break;
            case UriContent uc:
                Console.WriteLine($"  [{i}] URI: {uc.Uri} ({uc.MediaType})");
                break;
            default:
                Console.WriteLine($"  [{i}] {contents[i].GetType().Name}");
                break;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Types
// ═══════════════════════════════════════════════════════════════════════════
enum VideoProvider { OpenAI, Veo, Runway, Luma }

// ═══════════════════════════════════════════════════════════════════════════
// Inline provider implementations
// (In production, these would be separate NuGet packages or project references)
// ═══════════════════════════════════════════════════════════════════════════

// ─── Google Veo ────────────────────────────────────────────────────────────
internal sealed class GoogleVeoVideoGenerator : IVideoGenerator
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;

    public GoogleVeoVideoGenerator(string apiKey, string modelId, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _modelId = modelId;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<VideoGenerationOperation> GenerateAsync(
        VideoGenerationRequest request, VideoGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        string model = options?.ModelId ?? _modelId;
        var body = new JsonObject();
        if (request.Prompt is not null) body["prompt"] = request.Prompt;

        if (request.OperationKind == VideoOperationKind.Create && request.OriginalMedia is not null)
        {
            foreach (var item in request.OriginalMedia)
            {
                if (item is DataContent dc && (dc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false) && dc.Data.Length > 0)
                {
                    body["image"] = new JsonObject { ["imageBytes"] = Convert.ToBase64String(dc.Data.ToArray()), ["mimeType"] = dc.MediaType };
                    break;
                }

                if (item is UriContent uc)
                {
                    body["image"] = new JsonObject { ["imageUri"] = uc.Uri.ToString() };
                    break;
                }
            }
        }

        var config = new JsonObject();
        if (options?.Duration is { } dur) config["durationSeconds"] = ((int)dur.TotalSeconds).ToString();
        if (options?.AspectRatio is { } ar) config["aspectRatio"] = ar;
        if (options?.Count is { } cnt) config["numberOfVideos"] = cnt;
        if (options?.Seed is int seed) config["seed"] = seed;
        if (options?.GenerateAudio == true) config["generateAudio"] = true;
        if (request.NegativePrompt is { } neg) config["negativePrompt"] = neg;
        if (options?.AdditionalProperties?.TryGetValue("personGeneration", out object? pg) == true && pg is string pgs) config["personGeneration"] = pgs;
        if (config.Count > 0) body["generationConfig"] = config;

        string url = $"{BaseUrl}/models/{model}:generateVideos?key={_apiKey}";
        using var content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = JsonDocument.Parse(responseBody);
        string opName = result.RootElement.GetProperty("name").GetString()!;
        return new GoogleVeoVideoGenerationOperation(opName, _apiKey, _httpClient, model);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() => _httpClient.Dispose();
}

internal sealed class GoogleVeoVideoGenerationOperation : VideoGenerationOperation
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private bool _done;
    private string? _status;
    private string? _failureReason;
    private readonly List<string> _videoUris = [];

    public GoogleVeoVideoGenerationOperation(string opName, string apiKey, HttpClient httpClient, string modelId)
    {
        OperationId = opName; ModelId = modelId; _apiKey = apiKey; _httpClient = httpClient; _status = "PROCESSING";
    }

    public override string? OperationId { get; }
    public override string? Status => _status;
    public override int? PercentComplete => _done ? 100 : null;
    public override bool IsCompleted => _done;
    public override string? FailureReason => _failureReason;

    public override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        using var resp = await _httpClient.GetAsync($"{BaseUrl}/{OperationId}?key={_apiKey}", cancellationToken);
        string body = await resp.Content.ReadAsStringAsync(cancellationToken);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        _done = root.TryGetProperty("done", out var d) && d.GetBoolean();
        if (_done) _status = "COMPLETED";
        if (root.TryGetProperty("error", out var err)) { _failureReason = err.ToString(); _status = "FAILED"; _done = true; }
        _videoUris.Clear();
        if (root.TryGetProperty("response", out var response) && response.TryGetProperty("generatedVideos", out var vids))
            foreach (var v in vids.EnumerateArray())
                if (v.TryGetProperty("video", out var video) && video.TryGetProperty("uri", out var uri))
                    _videoUris.Add(uri.GetString()!);
    }

    public override async Task WaitForCompletionAsync(IProgress<VideoGenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        while (!IsCompleted)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            await UpdateAsync(cancellationToken);
            progress?.Report(new VideoGenerationProgress(_status, PercentComplete));
        }

        if (_status == "FAILED") throw new InvalidOperationException($"Video generation failed: {_failureReason}");
    }

    public override async Task<IList<AIContent>> GetContentsAsync(VideoGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!IsCompleted) throw new InvalidOperationException("Not completed.");
        if (_videoUris.Count == 0) await UpdateAsync(cancellationToken);
        var results = new List<AIContent>();
        foreach (var uri in _videoUris)
        {
            if (options?.ResponseFormat == VideoGenerationResponseFormat.Uri) { results.Add(new UriContent(new Uri(uri), "video/mp4")); continue; }
            using var r = await _httpClient.GetAsync(uri, cancellationToken); r.EnsureSuccessStatusCode();
            results.Add(new DataContent(await r.Content.ReadAsByteArrayAsync(cancellationToken), "video/mp4"));
        }

        return results;
    }
}

// ─── Runway ────────────────────────────────────────────────────────────────
internal sealed class RunwayVideoGenerator : IVideoGenerator
{
    private const string BaseUrl = "https://api.dev.runwayml.com";
    private readonly HttpClient _httpClient;
    private readonly string _modelId;

    public RunwayVideoGenerator(string apiKey, string modelId, HttpClient? httpClient = null)
    {
        _modelId = modelId;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-Runway-Version", "2024-11-06");
    }

    public async Task<VideoGenerationOperation> GenerateAsync(
        VideoGenerationRequest request, VideoGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        string model = options?.ModelId ?? _modelId;
        string endpoint;
        JsonObject body;

        bool hasVideo = request.OriginalMedia?.Any(m => m is DataContent dc && dc.MediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true) == true;
        bool hasImage = request.OriginalMedia?.Any(m => m is DataContent dc && dc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true) == true;

        if (request.OperationKind == VideoOperationKind.Edit && hasVideo)
        {
            endpoint = "/v1/video_to_video";
            body = new JsonObject { ["model"] = "gen4_aleph", ["promptText"] = request.Prompt ?? "" };
            string? vidUri = GetMediaUri(request.OriginalMedia, "video/");
            if (vidUri is not null) body["videoUri"] = vidUri;
        }
        else if (hasImage)
        {
            endpoint = "/v1/image_to_video";
            string? imgUri = GetMediaUri(request.OriginalMedia, "image/");
            string ratio = options?.AspectRatio is { } ar ? MapAspectRatio(ar) : "1280:720";
            body = new JsonObject { ["model"] = model, ["promptText"] = request.Prompt ?? "", ["promptImage"] = imgUri ?? "", ["ratio"] = ratio };
        }
        else
        {
            endpoint = "/v1/text_to_video";
            string ratio = options?.AspectRatio is { } ar ? MapAspectRatio(ar) : "1280:720";
            body = new JsonObject { ["model"] = model, ["promptText"] = request.Prompt ?? "", ["ratio"] = ratio };
        }

        if (options?.Duration is { } dur) body["duration"] = (int)dur.TotalSeconds;
        if (options?.Seed is int seed) body["seed"] = seed;

        using var content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        using var resp = await _httpClient.PostAsync($"{BaseUrl}{endpoint}", content, cancellationToken);
        string responseBody = await resp.Content.ReadAsStringAsync(cancellationToken);
        resp.EnsureSuccessStatusCode();
        string taskId = JsonDocument.Parse(responseBody).RootElement.GetProperty("id").GetString()!;
        return new RunwayVideoGenerationOperation(taskId, _httpClient, model);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() => _httpClient.Dispose();

    private static string? GetMediaUri(IEnumerable<AIContent>? media, string prefix)
    {
        if (media is null) return null;
        foreach (var item in media)
        {
            if (item is UriContent uc) return uc.Uri.ToString();
            if (item is DataContent dc && dc.Data.Length > 0)
                return dc.Uri ?? $"data:{dc.MediaType ?? "application/octet-stream"};base64,{Convert.ToBase64String(dc.Data.ToArray())}";
        }

        return null;
    }

    private static string MapAspectRatio(string ar) => ar switch
    {
        "16:9" => "1280:720", "9:16" => "720:1280", "1:1" => "960:960",
        "4:3" => "1104:832", "3:4" => "832:1104", _ => ar,
    };
}

internal sealed class RunwayVideoGenerationOperation : VideoGenerationOperation
{
    private const string BaseUrl = "https://api.dev.runwayml.com";
    private readonly HttpClient _httpClient;
    private string? _status;
    private string? _failureReason;
    private string? _outputUrl;

    public RunwayVideoGenerationOperation(string taskId, HttpClient httpClient, string modelId)
    {
        OperationId = taskId; ModelId = modelId; _httpClient = httpClient; _status = "PENDING";
    }

    public override string? OperationId { get; }
    public override string? Status => _status;
    public override int? PercentComplete => _status switch { "SUCCEEDED" => 100, "RUNNING" => 50, "THROTTLED" => 10, _ => 0 };
    public override bool IsCompleted => _status is "SUCCEEDED" or "FAILED";
    public override string? FailureReason => _failureReason;

    public override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        using var resp = await _httpClient.GetAsync($"{BaseUrl}/v1/tasks/{OperationId}", cancellationToken);
        string body = await resp.Content.ReadAsStringAsync(cancellationToken);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        _status = root.GetProperty("status").GetString();
        if (root.TryGetProperty("failure", out var f) && f.ValueKind == JsonValueKind.String) _failureReason = f.GetString();
        if (root.TryGetProperty("output", out var o) && o.ValueKind == JsonValueKind.Array && o.GetArrayLength() > 0) _outputUrl = o[0].GetString();
        else if (root.TryGetProperty("output", out var o2) && o2.ValueKind == JsonValueKind.String) _outputUrl = o2.GetString();
    }

    public override async Task WaitForCompletionAsync(IProgress<VideoGenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        while (!IsCompleted)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await UpdateAsync(cancellationToken);
            progress?.Report(new VideoGenerationProgress(_status, PercentComplete));
        }

        if (_status == "FAILED") throw new InvalidOperationException($"Task failed: {_failureReason}");
    }

    public override async Task<IList<AIContent>> GetContentsAsync(VideoGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!IsCompleted) throw new InvalidOperationException("Not completed.");
        if (_outputUrl is null) await UpdateAsync(cancellationToken);
        if (_outputUrl is null) throw new InvalidOperationException("No output URL.");
        if (options?.ResponseFormat == VideoGenerationResponseFormat.Uri) return [new UriContent(new Uri(_outputUrl), "video/mp4")];
        using var r = await _httpClient.GetAsync(_outputUrl, cancellationToken); r.EnsureSuccessStatusCode();
        return [new DataContent(await r.Content.ReadAsByteArrayAsync(cancellationToken), "video/mp4")];
    }
}

// ─── Luma AI ───────────────────────────────────────────────────────────────
internal sealed class LumaVideoGenerator : IVideoGenerator
{
    private const string BaseUrl = "https://api.lumalabs.ai/dream-machine/v1";
    private readonly HttpClient _httpClient;
    private readonly string _modelId;

    public LumaVideoGenerator(string apiKey, string modelId, HttpClient? httpClient = null)
    {
        _modelId = modelId;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<VideoGenerationOperation> GenerateAsync(
        VideoGenerationRequest request, VideoGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        string model = options?.ModelId ?? _modelId;
        var body = new JsonObject { ["prompt"] = request.Prompt, ["model"] = model };
        if (options?.Duration is { } dur) body["duration"] = $"{(int)dur.TotalSeconds}s";
        if (options?.AspectRatio is { } ar) body["aspect_ratio"] = ar;

        var keyframes = new JsonObject();
        if (request.OperationKind == VideoOperationKind.Create && request.OriginalMedia is not null)
        {
            int idx = 0;
            foreach (var item in request.OriginalMedia)
            {
                string key = idx == 0 ? "frame0" : "frame1";
                if (item is UriContent uc) keyframes[key] = new JsonObject { ["type"] = "image", ["url"] = uc.Uri.ToString() };
                else if (item is DataContent dc && dc.Data.Length > 0)
                    keyframes[key] = new JsonObject { ["type"] = "image", ["url"] = dc.Uri ?? $"data:{dc.MediaType ?? "image/png"};base64,{Convert.ToBase64String(dc.Data.ToArray())}" };
                if (++idx >= 2) break;
            }
        }
        else if (request.OperationKind == VideoOperationKind.Extend && request.SourceVideoId is not null)
        {
            keyframes["frame0"] = new JsonObject { ["type"] = "generation", ["id"] = request.SourceVideoId };
        }

        if (keyframes.Count > 0) body["keyframes"] = keyframes;

        using var content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        using var resp = await _httpClient.PostAsync($"{BaseUrl}/generations", content, cancellationToken);
        string responseBody = await resp.Content.ReadAsStringAsync(cancellationToken);
        resp.EnsureSuccessStatusCode();
        string opId = JsonDocument.Parse(responseBody).RootElement.GetProperty("id").GetString()!;
        return new LumaVideoGenerationOperation(opId, _httpClient, model);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() => _httpClient.Dispose();
}

internal sealed class LumaVideoGenerationOperation : VideoGenerationOperation
{
    private const string BaseUrl = "https://api.lumalabs.ai/dream-machine/v1";
    private readonly HttpClient _httpClient;
    private string? _status;
    private string? _failureReason;
    private string? _videoUrl;

    public LumaVideoGenerationOperation(string opId, HttpClient httpClient, string modelId)
    {
        OperationId = opId; ModelId = modelId; _httpClient = httpClient; _status = "queued";
    }

    public override string? OperationId { get; }
    public override string? Status => _status;
    public override int? PercentComplete => _status switch { "completed" => 100, "dreaming" => 50, _ => 0 };
    public override bool IsCompleted => _status is "completed" or "failed";
    public override string? FailureReason => _failureReason;

    public override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        using var resp = await _httpClient.GetAsync($"{BaseUrl}/generations/{OperationId}", cancellationToken);
        string body = await resp.Content.ReadAsStringAsync(cancellationToken);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        _status = root.GetProperty("state").GetString();
        if (root.TryGetProperty("failure_reason", out var fr) && fr.ValueKind == JsonValueKind.String) _failureReason = fr.GetString();
        if (root.TryGetProperty("assets", out var assets) && assets.TryGetProperty("video", out var v) && v.ValueKind == JsonValueKind.String) _videoUrl = v.GetString();
    }

    public override async Task WaitForCompletionAsync(IProgress<VideoGenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        while (!IsCompleted)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await UpdateAsync(cancellationToken);
            progress?.Report(new VideoGenerationProgress(_status, PercentComplete));
        }

        if (_status == "failed") throw new InvalidOperationException($"Generation failed: {_failureReason}");
    }

    public override async Task<IList<AIContent>> GetContentsAsync(VideoGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!IsCompleted) throw new InvalidOperationException("Not completed.");
        if (_videoUrl is null) await UpdateAsync(cancellationToken);
        if (_videoUrl is null) throw new InvalidOperationException("No video URL.");
        if (options?.ResponseFormat == VideoGenerationResponseFormat.Uri) return [new UriContent(new Uri(_videoUrl), "video/mp4")];
        using var r = await _httpClient.GetAsync(_videoUrl, cancellationToken); r.EnsureSuccessStatusCode();
        return [new DataContent(await r.Content.ReadAsByteArrayAsync(cancellationToken), "video/mp4")];
    }

    public override VideoGenerationRequest CreateExtensionRequest(string? prompt = null)
        => new() { Prompt = prompt, SourceVideoId = OperationId, OperationKind = VideoOperationKind.Extend };
}
