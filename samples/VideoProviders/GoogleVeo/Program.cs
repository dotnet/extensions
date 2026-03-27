// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Google Veo (Gemini API) Video Generation Sample
//
// Usage examples:
//   dotnet run -- generate "A cinematic drone shot of a coastline at sunset"
//   dotnet run -- generate "A character walks through" --image reference.jpg
//   dotnet run -- generate "Smooth transition" --image first.jpg --last-frame last.jpg
//
// Environment:
//   GOOGLE_API_KEY — your Gemini API key from https://aistudio.google.com/apikey

using System.CommandLine;
using System.Text.Json.Nodes;
using GoogleVeo;
using Microsoft.Extensions.AI;

var modelOption = new Option<string>("--model", () => "veo-3.1-generate-preview",
    "Model: veo-3.1-generate-preview, veo-3.1-fast-preview, veo-3, veo-2.");
var outputOption = new Option<string?>("--output", "Output file path (.mp4).");
var durationOption = new Option<int?>("--duration", "Duration in seconds (4, 6, or 8).");
var resolutionOption = new Option<string>("--resolution", () => "720p", "Resolution: 720p, 1080p, 4k.");
var aspectRatioOption = new Option<string?>("--aspect-ratio", "Aspect ratio (e.g. 16:9, 9:16).");
var formatOption = new Option<string>("--format", () => "data", "Response format: data or uri.");
var countOption = new Option<int>("--count", () => 1, "Number of videos to generate.");
var negativePromptOption = new Option<string?>("--negative-prompt", "What to avoid in the video.");
var audioOption = new Option<bool>("--audio", () => false, "Generate audio (Veo 3+ only).");
var seedOption = new Option<int?>("--seed", "Seed for reproducibility.");
var personGenOption = new Option<string?>("--person-generation", "Person generation policy: dont_allow, allow_adult.");

// ── generate ────────────────────────────────────────────────────────────────
var promptArg = new Argument<string>("prompt", "Text prompt.");
var imageOption = new Option<string?>("--image", "Input image for image-to-video.");
var lastFrameOption = new Option<string?>("--last-frame", "Last frame image for interpolation.");
var refImagesOption = new Option<string[]?>("--ref-image", "Reference image(s) for style guidance (up to 3).") { AllowMultipleArgumentsPerToken = true };
var refTypeOption = new Option<string>("--ref-type", () => "style", "Reference type: style or subject.");

var generateCommand = new Command("generate", "Generate video from text, image-to-video, or frame interpolation.")
{
    promptArg, modelOption, outputOption, imageOption, lastFrameOption,
    refImagesOption, refTypeOption,
    durationOption, resolutionOption, aspectRatioOption, countOption,
    negativePromptOption, audioOption, seedOption, personGenOption, formatOption,
};

generateCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(promptArg);
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    string? imagePath = context.ParseResult.GetValueForOption(imageOption);
    string? lastFrame = context.ParseResult.GetValueForOption(lastFrameOption);
    string[]? refImages = context.ParseResult.GetValueForOption(refImagesOption);
    string refType = context.ParseResult.GetValueForOption(refTypeOption)!;
    int? duration = context.ParseResult.GetValueForOption(durationOption);
    string resolution = context.ParseResult.GetValueForOption(resolutionOption)!;
    string? aspectRatio = context.ParseResult.GetValueForOption(aspectRatioOption);
    int count = context.ParseResult.GetValueForOption(countOption);
    string? negativePrompt = context.ParseResult.GetValueForOption(negativePromptOption);
    bool audio = context.ParseResult.GetValueForOption(audioOption);
    int? seed = context.ParseResult.GetValueForOption(seedOption);
    string? personGen = context.ParseResult.GetValueForOption(personGenOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);

    List<AIContent>? media = null;
    if (imagePath is not null)
    {
        media = [await DataContent.LoadFromAsync(imagePath)];
    }

    var options = new VideoGenerationOptions
    {
        ModelId = model,
        Count = count,
        ResponseFormat = string.Equals(format, "uri", StringComparison.OrdinalIgnoreCase)
            ? VideoGenerationResponseFormat.Uri
            : VideoGenerationResponseFormat.Data,
        AdditionalProperties = [],
    };

    if (duration.HasValue)
    {
        options.Duration = TimeSpan.FromSeconds(duration.Value);
    }

    options.VideoSize = resolution switch
    {
        "1080p" => new System.Drawing.Size(1920, 1080),
        "4k" => new System.Drawing.Size(3840, 2160),
        _ => new System.Drawing.Size(1280, 720),
    };

    if (aspectRatio is not null)
    {
        options.AspectRatio = aspectRatio;
    }

    if (audio)
    {
        options.GenerateAudio = true;
    }

    if (seed.HasValue)
    {
        options.Seed = seed.Value;
    }

    if (personGen is not null)
    {
        options.AdditionalProperties["personGeneration"] = personGen;
    }

    // Last frame for interpolation
    if (lastFrame is not null)
    {
        byte[] lastFrameBytes = await File.ReadAllBytesAsync(lastFrame);
        var lastFrameNode = new JsonObject
        {
            ["imageBytes"] = Convert.ToBase64String(lastFrameBytes),
            ["mimeType"] = "image/png",
        };
        options.AdditionalProperties["lastFrameImage"] = lastFrameNode;
    }

    // Reference images
    if (refImages is { Length: > 0 })
    {
        var refs = new JsonArray();
        foreach (string refImg in refImages)
        {
            byte[] refBytes = await File.ReadAllBytesAsync(refImg);
            refs.Add(new JsonObject
            {
                ["referenceImage"] = new JsonObject
                {
                    ["imageBytes"] = Convert.ToBase64String(refBytes),
                    ["mimeType"] = "image/png",
                },
                ["referenceType"] = refType.ToUpperInvariant(),
            });
        }

        options.AdditionalProperties["referenceImages"] = refs;
    }

    var request = new VideoGenerationRequest(prompt, media);

    if (negativePrompt is not null)
    {
        request.NegativePrompt = negativePrompt;
    }

    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── Root ────────────────────────────────────────────────────────────────────
var rootCommand = new RootCommand("Google Veo (Gemini API) video generation sample using MEAI IVideoGenerator.")
{
    generateCommand,
};

return await rootCommand.InvokeAsync(args);

// ═══════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════
static IVideoGenerator CreateGenerator(string model)
{
    string? apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.Error.WriteLine("Error: Set the GOOGLE_API_KEY environment variable.");
        Console.Error.WriteLine("Get a key at https://aistudio.google.com/apikey");
        Environment.Exit(1);
    }

    return new GoogleVeoVideoGenerator(apiKey, model);
}

static async Task CompleteAndSaveAsync(VideoGenerationOperation operation, VideoGenerationOptions options, string? outputPath)
{
    Console.WriteLine($"OPERATION: {operation.OperationId}");
    Console.WriteLine($"  Status: {operation.Status}");

    var sw = System.Diagnostics.Stopwatch.StartNew();
    await operation.WaitForCompletionAsync(
        new Progress<VideoGenerationProgress>(p =>
            Console.WriteLine($"  Progress: {p.Status}{(p.PercentComplete.HasValue ? $" ({p.PercentComplete}%)" : "")}")));

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
                Console.WriteLine($"  [{i}] Saved: {savePath} ({dc.Data.Length} bytes)");
                break;
            case DataContent dc:
                Console.WriteLine($"  [{i}] DataContent: {dc.Data.Length} bytes ({dc.MediaType})");
                break;
            case UriContent uc:
                Console.WriteLine($"  [{i}] URI: {uc.Uri}");
                break;
            default:
                Console.WriteLine($"  [{i}] {contents[i].GetType().Name}");
                break;
        }
    }
}
