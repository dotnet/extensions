// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Video Generation POC — Microsoft.Extensions.AI general-purpose CLI
//
// Usage examples:
//   dotnet run -- generate "A cat playing piano"
//   dotnet run -- generate "She turns and smiles" --input reference.jpg
//   dotnet run -- generate "A tracking shot of DotBot" --character char_abc123
//   dotnet run -- upload-character DotBot --input clip.mp4
//   dotnet run -- edit "Change the sky to sunset" --video video_abc123
//   dotnet run -- extend "Continue the scene" --video video_abc123
//
// All commands print machine-parseable lines (OPERATION_ID, CHARACTER_ID) for scripting.

using System.CommandLine;
using System.Drawing;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

// ── Shared options ─────────────────────────────────────────────────────────
var modelOption = new Option<string>("--model", () => "sora-2", "Model ID.");
var outputOption = new Option<string?>("--output", "Output file path (.mp4). Omit for URI-only display.");
var durationOption = new Option<int?>("--duration", "Duration in seconds.");
var widthOption = new Option<int?>("--width", () => 1280, "Video width.");
var heightOption = new Option<int?>("--height", () => 720, "Video height.");
var formatOption = new Option<string>("--format", () => "data", "Response format: data or uri.");

// ── generate ───────────────────────────────────────────────────────────────
var generatePromptArg = new Argument<string>("prompt", "Text prompt describing the video to generate.");
var inputOption = new Option<string[]>("--input", "Input file(s) — images for image-to-video, or a video for editing.") { AllowMultipleArgumentsPerToken = true };
var characterOption = new Option<string[]>("--character", "Character ID(s) to include.") { AllowMultipleArgumentsPerToken = true };

var generateCommand = new Command("generate", "Generate a new video from a text prompt (optionally with input images and characters).")
{
    generatePromptArg, modelOption, outputOption, inputOption, characterOption, durationOption, widthOption, heightOption, formatOption,
};

generateCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(generatePromptArg);
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    string[] inputPaths = context.ParseResult.GetValueForOption(inputOption) ?? [];
    string[] characterIds = context.ParseResult.GetValueForOption(characterOption) ?? [];
    int? duration = context.ParseResult.GetValueForOption(durationOption);
    int? width = context.ParseResult.GetValueForOption(widthOption);
    int? height = context.ParseResult.GetValueForOption(heightOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);

    List<AIContent>? inputMedia = await LoadInputFilesAsync(inputPaths);
    if (inputMedia is null && inputPaths.Length > 0)
    {
        context.ExitCode = 1;
        return;
    }

    var options = BuildOptions(duration, width, height, format, characterIds);
    var request = new VideoGenerationRequest(prompt);
    if (inputMedia is { Count: > 0 })
    {
        request.StartFrame = inputMedia[0];
    }

    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── upload-character ───────────────────────────────────────────────────────
var charNameArg = new Argument<string>("name", "Name for the character (mention this name verbatim in prompts).");
var charInputOption = new Option<string>("--input", "Video file (.mp4) to upload as the character source.") { IsRequired = true };

var uploadCharCommand = new Command("upload-character", "Upload a video clip as a reusable character asset.")
{
    charNameArg, charInputOption, modelOption,
};

uploadCharCommand.SetHandler(async (context) =>
{
    string name = context.ParseResult.GetValueForArgument(charNameArg);
    string inputPath = context.ParseResult.GetValueForOption(charInputOption)!;
    string model = context.ParseResult.GetValueForOption(modelOption)!;

    if (!File.Exists(inputPath))
    {
        Console.Error.WriteLine($"Error: File not found: {inputPath}");
        context.ExitCode = 1;
        return;
    }

    using var generator = CreateGenerator(model);
    DataContent videoContent = await DataContent.LoadFromAsync(inputPath);

    Console.WriteLine($"Uploading character '{name}' from {inputPath} ({videoContent.Data.Length} bytes)...");
    string characterId = await generator.UploadVideoCharacterAsync(name, videoContent);

    Console.WriteLine($"CHARACTER_ID: {characterId}");
});

// ── edit ───────────────────────────────────────────────────────────────────
var editPromptArg = new Argument<string>("prompt", "Prompt describing the edit to apply.");
var editVideoOption = new Option<string>("--video", "Video ID of the generation to edit.") { IsRequired = true };

var editCommand = new Command("edit", "Edit an existing video by ID.")
{
    editPromptArg, editVideoOption, modelOption, outputOption, formatOption,
};

editCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(editPromptArg);
    string videoId = context.ParseResult.GetValueForOption(editVideoOption)!;
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);

    var options = BuildOptions(duration: null, width: null, height: null, format, characterIds: []);
    var request = new VideoGenerationRequest(prompt)
    {
        OperationKind = VideoOperationKind.Edit,
        SourceVideoId = videoId,
    };

    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── extend ─────────────────────────────────────────────────────────────────
var extendPromptArg = new Argument<string>("prompt", "Prompt describing how the scene should continue.");
var extendVideoOption = new Option<string>("--video", "Video ID of the completed video to extend.") { IsRequired = true };

var extendCommand = new Command("extend", "Extend a completed video by ID.")
{
    extendPromptArg, extendVideoOption, modelOption, outputOption, durationOption, formatOption,
};

extendCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(extendPromptArg);
    string videoId = context.ParseResult.GetValueForOption(extendVideoOption)!;
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string? outputPath = context.ParseResult.GetValueForOption(outputOption);
    int? duration = context.ParseResult.GetValueForOption(durationOption);
    string format = context.ParseResult.GetValueForOption(formatOption)!;

    using var generator = CreateGenerator(model);

    var options = BuildOptions(duration, width: null, height: null, format, characterIds: []);
    var request = new VideoGenerationRequest(prompt)
    {
        OperationKind = VideoOperationKind.Extend,
        SourceVideoId = videoId,
    };

    var operation = await generator.GenerateAsync(request, options);
    await CompleteAndSaveAsync(operation, options, outputPath);
});

// ── Root command ───────────────────────────────────────────────────────────
var rootCommand = new RootCommand("Video Generation POC — Microsoft.Extensions.AI CLI for video generation, editing, extending, and character management.")
{
    generateCommand,
    uploadCharCommand,
    editCommand,
    extendCommand,
};

return await rootCommand.InvokeAsync(args);

// ═══════════════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════════════
static IVideoGenerator CreateGenerator(string model)
{
    string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.Error.WriteLine("Error: Set the OPENAI_API_KEY environment variable.");
        Environment.Exit(1);
    }

    var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
    var openAIClient = new OpenAIClient(apiKey);

    return openAIClient
        .GetVideoClient()
        .AsIVideoGenerator(model)
        .AsBuilder()
        .UseLogging(loggerFactory)
        .UseOpenTelemetry(loggerFactory)
        .Build();
}

static VideoGenerationOptions BuildOptions(int? duration, int? width, int? height, string format, string[] characterIds)
{
    var options = new VideoGenerationOptions
    {
        ResponseFormat = string.Equals(format, "uri", StringComparison.OrdinalIgnoreCase)
            ? VideoGenerationResponseFormat.Uri
            : VideoGenerationResponseFormat.Data,
    };

    if (duration.HasValue)
    {
        options.Duration = TimeSpan.FromSeconds(duration.Value);
    }

    if (width.HasValue && height.HasValue)
    {
        options.VideoSize = new Size(width.Value, height.Value);
    }

    if (characterIds.Length > 0)
    {
        var chars = new JsonArray();
        foreach (string charId in characterIds)
        {
            chars.Add(new JsonObject { ["id"] = charId });
        }

        options.AdditionalProperties = new() { ["characters"] = chars };
    }

    return options;
}

static async Task<List<AIContent>?> LoadInputFilesAsync(string[] inputPaths)
{
    if (inputPaths.Length == 0)
    {
        return null;
    }

    var media = new List<AIContent>();
    foreach (string path in inputPaths)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Error: Input file not found: {path}");
            return null;
        }

        DataContent loaded = await DataContent.LoadFromAsync(path);
        media.Add(loaded);
        Console.WriteLine($"  Loaded: {path} ({loaded.MediaType}, {loaded.Data.Length} bytes)");
    }

    return media;
}

static async Task CompleteAndSaveAsync(VideoGenerationOperation operation, VideoGenerationOptions options, string? outputPath)
{
    Console.WriteLine($"OPERATION_ID: {operation.OperationId}");
    Console.WriteLine($"  Status: {operation.Status}");

    var sw = System.Diagnostics.Stopwatch.StartNew();
    await operation.WaitForCompletionAsync(
        new Progress<VideoGenerationProgress>(p =>
            Console.WriteLine($"  Progress: {p.Status}{(p.PercentComplete.HasValue ? $" ({p.PercentComplete}%)" : string.Empty)}")));

    sw.Stop();
    Console.WriteLine($"  Completed in {sw.Elapsed.TotalSeconds:F1}s");

    if (operation.Usage is { } usage)
    {
        Console.WriteLine($"  Tokens: input={usage.InputTokenCount}, output={usage.OutputTokenCount}, total={usage.TotalTokenCount}");
    }

    var contents = await operation.GetContentsAsync(options);
    Console.WriteLine($"  {contents.Count} content item(s)");

    for (int i = 0; i < contents.Count; i++)
    {
        switch (contents[i])
        {
            case DataContent dc:
                if (outputPath is not null)
                {
                    string filePath = contents.Count == 1
                        ? outputPath
                        : Path.Combine(
                            Path.GetDirectoryName(outputPath) ?? ".",
                            $"{Path.GetFileNameWithoutExtension(outputPath)}_{i}{Path.GetExtension(outputPath)}");

                    string? dir = Path.GetDirectoryName(filePath);
                    if (dir is not null)
                    {
                        Directory.CreateDirectory(dir);
                    }

                    await dc.SaveToAsync(filePath);
                    Console.WriteLine($"  [{i}] Saved: {filePath} ({dc.Data.Length} bytes, {dc.MediaType})");
                }
                else
                {
                    Console.WriteLine($"  [{i}] DataContent: {dc.Data.Length} bytes ({dc.MediaType})");
                }

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
