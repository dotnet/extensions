// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runway Video Generation Sample
//
// Usage examples:
//   dotnet run -- text-to-video "A cute bunny hopping in a meadow"
//   dotnet run -- image-to-video "The scene comes alive" --image bunny.jpg
//   dotnet run -- video-to-video "Add easter elements" --video https://example.com/bunny.mp4
//
// Environment:
//   RUNWAY_API_KEY — your Runway API key from https://dev.runwayml.com/

using System.CommandLine;
using Microsoft.Extensions.AI;
using Runway;

var modelOption = new Option<string>("--model", () => "gen4_turbo", "Model (gen4.5, gen4_turbo, gen4_aleph, veo3.1, veo3).");
var outputOption = new Option<string?>("--output", "Output file path (.mp4).");
var durationOption = new Option<int?>("--duration", "Duration in seconds (2-10).");
var seedOption = new Option<int?>("--seed", "Seed for reproducibility.");
var formatOption = new Option<string>("--format", () => "data", "Response format: data or uri.");

// ── text-to-video ───────────────────────────────────────────────────────────
var t2vPromptArg = new Argument<string>("prompt", "Text prompt.");
var t2vRatioOption = new Option<string>("--ratio", () => "1280:720", "Output ratio (1280:720, 720:1280).");

var t2vCommand = new Command("text-to-video", "Generate video from text only.")
{
    t2vPromptArg, modelOption, outputOption, durationOption, seedOption, t2vRatioOption, formatOption,
};

t2vCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(t2vPromptArg);
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    int? duration = context.ParseResult.GetValueForOption(durationOption);
    int? seed = context.ParseResult.GetValueForOption(seedOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);
    var options = BuildOptions(model, duration, seed, format);
    var request = new VideoGenerationRequest(prompt);
    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── image-to-video ──────────────────────────────────────────────────────────
var i2vPromptArg = new Argument<string>("prompt", "Text prompt to describe the video.");
var i2vImageOption = new Option<string>("--image", "Input image (file path or HTTPS URL).") { IsRequired = true };

var i2vCommand = new Command("image-to-video", "Generate video from an image + prompt.")
{
    i2vPromptArg, i2vImageOption, modelOption, outputOption, durationOption, seedOption, formatOption,
};

i2vCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(i2vPromptArg);
    string imagePath = context.ParseResult.GetValueForOption(i2vImageOption)!;
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    int? duration = context.ParseResult.GetValueForOption(durationOption);
    int? seed = context.ParseResult.GetValueForOption(seedOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);

    List<AIContent> media;
    if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
    {
        media = [new UriContent(new Uri(imagePath), "image/jpeg")];
    }
    else
    {
        media = [await DataContent.LoadFromAsync(imagePath)];
    }

    var options = BuildOptions(model, duration, seed, format);
    var request = new VideoGenerationRequest(prompt, media);
    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── video-to-video ──────────────────────────────────────────────────────────
var v2vPromptArg = new Argument<string>("prompt", "Prompt describing the style transfer.");
var v2vVideoOption = new Option<string>("--video", "Source video (file path or HTTPS URL).") { IsRequired = true };
var v2vRefImageOption = new Option<string?>("--reference", "Reference image for style guidance.");

var v2vCommand = new Command("video-to-video", "Transform a video with gen4_aleph.")
{
    v2vPromptArg, v2vVideoOption, v2vRefImageOption, outputOption, seedOption, formatOption,
};

v2vCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(v2vPromptArg);
    string videoPath = context.ParseResult.GetValueForOption(v2vVideoOption)!;
    string? refImage = context.ParseResult.GetValueForOption(v2vRefImageOption);
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    int? seed = context.ParseResult.GetValueForOption(seedOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator("gen4_aleph");

    List<AIContent> media;
    if (videoPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
    {
        media = [new UriContent(new Uri(videoPath), "video/mp4")];
    }
    else
    {
        media = [await DataContent.LoadFromAsync(videoPath)];
    }

    var options = BuildOptions("gen4_aleph", null, seed, format);

    // Add reference image if provided
    if (refImage is not null)
    {
        var refs = new System.Text.Json.Nodes.JsonArray
        {
            new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "image",
                ["uri"] = refImage.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? refImage
                    : $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(refImage))}",
            },
        };
        options.AdditionalProperties ??= [];
        options.AdditionalProperties["references"] = refs;
    }

    var request = new VideoGenerationRequest(prompt, media)
    {
        OperationKind = VideoOperationKind.Edit,
    };

    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── Root ────────────────────────────────────────────────────────────────────
var rootCommand = new RootCommand("Runway video generation sample using MEAI IVideoGenerator.")
{
    t2vCommand,
    i2vCommand,
    v2vCommand,
};

return await rootCommand.InvokeAsync(args);

// ═══════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════
static IVideoGenerator CreateGenerator(string model)
{
    string? apiKey = Environment.GetEnvironmentVariable("RUNWAY_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.Error.WriteLine("Error: Set the RUNWAY_API_KEY environment variable.");
        Console.Error.WriteLine("Get a key at https://dev.runwayml.com/");
        Environment.Exit(1);
    }

    return new RunwayVideoGenerator(apiKey, model);
}

static VideoGenerationOptions BuildOptions(string model, int? duration, int? seed, string format)
{
    var options = new VideoGenerationOptions
    {
        ModelId = model,
        ResponseFormat = string.Equals(format, "uri", StringComparison.OrdinalIgnoreCase)
            ? VideoGenerationResponseFormat.Uri
            : VideoGenerationResponseFormat.Data,
    };

    if (duration.HasValue)
    {
        options.Duration = TimeSpan.FromSeconds(duration.Value);
    }

    if (seed.HasValue)
    {
        options.Seed = seed.Value;
    }

    return options;
}

static async Task CompleteAndSaveAsync(VideoGenerationOperation operation, VideoGenerationOptions options, string? outputPath)
{
    Console.WriteLine($"TASK_ID: {operation.OperationId}");
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
        switch (contents[i])
        {
            case DataContent dc when outputPath is not null:
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
                await dc.SaveToAsync(outputPath);
                Console.WriteLine($"  [{i}] Saved: {outputPath} ({dc.Data.Length} bytes)");
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
