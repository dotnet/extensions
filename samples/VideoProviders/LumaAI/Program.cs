// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Luma AI (Dream Machine) Video Generation Sample
//
// Usage examples:
//   dotnet run -- generate "A tiger walking through snow"
//   dotnet run -- generate "The scene continues" --image start-frame.jpg
//   dotnet run -- extend "The tiger starts running" --video <generation-id>
//
// Environment:
//   LUMA_API_KEY — your Luma API key from https://lumalabs.ai/dream-machine/api/keys

using System.CommandLine;
using LumaAI;
using Microsoft.Extensions.AI;

var modelOption = new Option<string>("--model", () => "ray-2", "Model ID (ray-2 or ray-flash-2).");
var outputOption = new Option<string?>("--output", "Output file path (.mp4).");
var durationOption = new Option<string?>("--duration", "Duration string (e.g. '5s').");
var resolutionOption = new Option<string>("--resolution", () => "720p", "Resolution: 540p, 720p, 1080p, 4k.");
var aspectRatioOption = new Option<string?>("--aspect-ratio", "Aspect ratio (e.g. 16:9).");
var loopOption = new Option<bool>("--loop", () => false, "Whether to loop the video.");
var formatOption = new Option<string>("--format", () => "data", "Response format: data or uri.");

// ── generate ────────────────────────────────────────────────────────────────
var generatePromptArg = new Argument<string>("prompt", "Text prompt.");
var imageOption = new Option<string?>("--image", "Input image file for image-to-video (HTTPS URL or local file).");
var endImageOption = new Option<string?>("--end-image", "End frame image for keyframe interpolation.");

var generateCommand = new Command("generate", "Generate a video from text or image.")
{
    generatePromptArg, modelOption, outputOption, imageOption, endImageOption,
    durationOption, resolutionOption, aspectRatioOption, loopOption, formatOption,
};

generateCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(generatePromptArg);
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    string? imagePath = context.ParseResult.GetValueForOption(imageOption);
    string? endImagePath = context.ParseResult.GetValueForOption(endImageOption);
    string? duration = context.ParseResult.GetValueForOption(durationOption);
    string resolution = context.ParseResult.GetValueForOption(resolutionOption)!;
    string? aspectRatio = context.ParseResult.GetValueForOption(aspectRatioOption);
    bool loop = context.ParseResult.GetValueForOption(loopOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);

    List<AIContent>? media = null;
    if (imagePath is not null)
    {
        media = [await DataContent.LoadFromAsync(imagePath)];
        if (endImagePath is not null)
        {
            media.Add(await DataContent.LoadFromAsync(endImagePath));
        }
    }

    var options = BuildOptions(duration, resolution, aspectRatio, loop, format);
    var request = new VideoGenerationRequest(prompt, media);
    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── extend ──────────────────────────────────────────────────────────────────
var extendPromptArg = new Argument<string>("prompt", "Prompt for the extension.");
var extendVideoOption = new Option<string>("--video", "Generation ID to extend.") { IsRequired = true };

var extendCommand = new Command("extend", "Extend a completed video generation.")
{
    extendPromptArg, extendVideoOption, modelOption, outputOption, formatOption,
};

extendCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(extendPromptArg);
    string videoId = context.ParseResult.GetValueForOption(extendVideoOption)!;
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);
    var options = BuildOptions(null, "720p", null, false, format);
    var request = new VideoGenerationRequest(prompt)
    {
        OperationKind = VideoOperationKind.Extend,
        SourceVideoId = videoId,
    };

    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── Root ────────────────────────────────────────────────────────────────────
var rootCommand = new RootCommand("Luma AI (Dream Machine) video generation sample using MEAI IVideoGenerator.")
{
    generateCommand,
    extendCommand,
};

return await rootCommand.InvokeAsync(args);

// ═══════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════
static IVideoGenerator CreateGenerator(string model)
{
    string? apiKey = Environment.GetEnvironmentVariable("LUMA_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.Error.WriteLine("Error: Set the LUMA_API_KEY environment variable.");
        Console.Error.WriteLine("Get a key at https://lumalabs.ai/dream-machine/api/keys");
        Environment.Exit(1);
    }

    return new LumaVideoGenerator(apiKey, model);
}

static VideoGenerationOptions BuildOptions(string? duration, string resolution, string? aspectRatio, bool loop, string format)
{
    var options = new VideoGenerationOptions
    {
        ResponseFormat = string.Equals(format, "uri", StringComparison.OrdinalIgnoreCase)
            ? VideoGenerationResponseFormat.Uri
            : VideoGenerationResponseFormat.Data,
        AdditionalProperties = [],
    };

    if (duration is not null)
    {
        // Try to parse "5s" → 5 seconds
        if (int.TryParse(duration.TrimEnd('s'), out int secs))
        {
            options.Duration = TimeSpan.FromSeconds(secs);
        }
    }

    if (aspectRatio is not null)
    {
        options.AspectRatio = aspectRatio;
    }

    if (loop)
    {
        options.AdditionalProperties["loop"] = true;
    }

    // Map resolution string to VideoSize for the abstraction
    options.VideoSize = resolution switch
    {
        "540p" => new System.Drawing.Size(960, 540),
        "1080p" => new System.Drawing.Size(1920, 1080),
        "4k" => new System.Drawing.Size(3840, 2160),
        _ => new System.Drawing.Size(1280, 720),
    };

    return options;
}

static async Task CompleteAndSaveAsync(VideoGenerationOperation operation, VideoGenerationOptions options, string? outputPath)
{
    Console.WriteLine($"OPERATION_ID: {operation.OperationId}");
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
