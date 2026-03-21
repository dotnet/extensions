// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Video Generation POC - Microsoft.Extensions.AI
// Usage: set OPENAI_API_KEY environment variable, then run:
//   dotnet run -- "A cat playing piano"
//   dotnet run -- "She turns and smiles" --input reference.jpg
//   dotnet run -- "Change the sky to sunset" --edit video_abc123
//   dotnet run -- "Continue the scene" --extend video_abc123
//   dotnet run -- "A tracking shot of Mossy" --character char_abc123

using System.CommandLine;
using System.Drawing;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

var promptArg = new Argument<string>("prompt", () => "A serene lake at sunset with gentle ripples", "Text prompt describing the video to generate.");
var modelOption = new Option<string>("--model", () => "sora-2", "Model ID to use for video generation.");
var outputOption = new Option<string>("--output", () => $"video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4", "Output file path for the generated video.");
var inputOption = new Option<string[]>("--input", "Input file(s) — images for image-to-video, or a video for editing.") { AllowMultipleArgumentsPerToken = true };
var editOption = new Option<string?>("--edit", "Video ID of an existing generation to edit (POST /videos/edits).");
var extendOption = new Option<string?>("--extend", "Video ID of a completed video to extend (POST /videos/extensions).");
var characterOption = new Option<string[]>("--character", "Character ID(s) to include in the generation.") { AllowMultipleArgumentsPerToken = true };

var rootCommand = new RootCommand("Video Generation POC — demonstrates Microsoft.Extensions.AI video generation with OpenAI.")
{
    promptArg,
    modelOption,
    outputOption,
    inputOption,
    editOption,
    extendOption,
    characterOption,
};

rootCommand.SetHandler(async (context) =>
{
    string prompt = context.ParseResult.GetValueForArgument(promptArg);
    string model = context.ParseResult.GetValueForOption(modelOption)!;
    string outputPath = context.ParseResult.GetValueForOption(outputOption)!;
    string[] inputPaths = context.ParseResult.GetValueForOption(inputOption) ?? [];
    string? editVideoId = context.ParseResult.GetValueForOption(editOption);
    string? extendVideoId = context.ParseResult.GetValueForOption(extendOption);
    string[] characterIds = context.ParseResult.GetValueForOption(characterOption) ?? [];

    // --- API key ---
    string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.Error.WriteLine("Error: Set the OPENAI_API_KEY environment variable.");
        context.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Prompt:  {prompt}");
    Console.WriteLine($"Model:   {model}");
    Console.WriteLine($"Output:  {outputPath}");
    if (inputPaths.Length > 0)
    {
        Console.WriteLine($"Inputs:  {string.Join(", ", inputPaths)}");
    }

    if (editVideoId is not null)
    {
        Console.WriteLine($"Edit:    {editVideoId}");
    }

    if (extendVideoId is not null)
    {
        Console.WriteLine($"Extend:  {extendVideoId}");
    }

    if (characterIds.Length > 0)
    {
        Console.WriteLine($"Characters: {string.Join(", ", characterIds)}");
    }

    Console.WriteLine();

    // --- Create the video generator with middleware pipeline ---
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

    var openAIClient = new OpenAIClient(apiKey);
    using IVideoGenerator generator = openAIClient
        .GetVideoClient()
        .AsIVideoGenerator(model)
        .AsBuilder()
        .UseLogging(loggerFactory)
        .UseOpenTelemetry(loggerFactory)
        .ConfigureOptions(options =>
        {
            options.Count ??= 1;
            options.Duration ??= TimeSpan.FromSeconds(12);
            options.VideoSize ??= new Size(1280, 720);
        })
        .Build();

    // --- Show metadata ---
    var metadata = generator.GetService<VideoGeneratorMetadata>();
    if (metadata is not null)
    {
        Console.WriteLine($"Provider:      {metadata.ProviderName}");
        Console.WriteLine($"Endpoint:      {metadata.ProviderUri}");
        Console.WriteLine($"Default Model: {metadata.DefaultModelId}");
        Console.WriteLine();
    }

    // --- Build request ---
    List<AIContent>? originalMedia = null;
    if (inputPaths.Length > 0)
    {
        originalMedia = [];
        foreach (string inputPath in inputPaths)
        {
            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Error: Input file not found: {inputPath}");
                context.ExitCode = 1;
                return;
            }

            DataContent loaded = await DataContent.LoadFromAsync(inputPath);
            originalMedia.Add(loaded);
            Console.WriteLine($"  Loaded input: {inputPath} ({loaded.MediaType}, {loaded.Data.Length} bytes)");
        }

        Console.WriteLine();
    }

    // --- Generate video ---
    string mode =
        extendVideoId is not null ? "Extending" :
        editVideoId is not null ? "Editing (by video ID)" :
        originalMedia?.Exists(c => c is DataContent dc && dc.HasTopLevelMediaType("video")) == true ? "Editing (uploaded video)" :
        originalMedia is not null ? "Generating (image-to-video)" :
        "Generating (text-to-video)";
    Console.WriteLine($"{mode}...");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    var generateOptions = new VideoGenerationOptions
    {
        ResponseFormat = VideoGenerationResponseFormat.Data,
    };

    if (characterIds.Length > 0)
    {
        var chars = new JsonArray();
        foreach (string charId in characterIds)
        {
            chars.Add(new JsonObject { ["id"] = charId });
        }

        generateOptions.AdditionalProperties ??= [];
        generateOptions.AdditionalProperties["characters"] = chars;
    }

    var request = new VideoGenerationRequest(prompt, originalMedia);

    if (editVideoId is not null)
    {
        request.OperationKind = VideoOperationKind.Edit;
        request.SourceVideoId = editVideoId;
    }
    else if (extendVideoId is not null)
    {
        request.OperationKind = VideoOperationKind.Extend;
        request.SourceVideoId = extendVideoId;
    }

    var operation = await generator.GenerateAsync(request, generateOptions);

    Console.WriteLine($"  Operation ID: {operation.OperationId}");
    Console.WriteLine($"  Initial status: {operation.Status}");

    await operation.WaitForCompletionAsync(
        new Progress<VideoGenerationProgress>(p =>
            Console.WriteLine($"  Status: {p.Status}{(p.PercentComplete.HasValue ? $" ({p.PercentComplete}%)" : string.Empty)}")));

    stopwatch.Stop();
    Console.WriteLine($"Completed in {stopwatch.Elapsed.TotalSeconds:F1}s");
    Console.WriteLine();

    // --- Download and process contents ---
    if (operation.Usage is { } usage)
    {
        Console.WriteLine($"Token Usage: input={usage.InputTokenCount}, output={usage.OutputTokenCount}, total={usage.TotalTokenCount}");
    }

    var contents = await operation.GetContentsAsync(generateOptions);
    Console.WriteLine($"Generated {contents.Count} content item(s):");
    for (int i = 0; i < contents.Count; i++)
    {
        var content = contents[i];
        switch (content)
        {
            case DataContent dc:
                string filePath = contents.Count == 1
                    ? outputPath
                    : Path.Combine(
                        Path.GetDirectoryName(outputPath) ?? ".",
                        $"{Path.GetFileNameWithoutExtension(outputPath)}_{i}{Path.GetExtension(outputPath)}");

                await dc.SaveToAsync(filePath);
                Console.WriteLine($"  [{i}] Saved {dc.Data.Length} bytes ({dc.MediaType}) -> {filePath}");
                break;

            case UriContent uc:
                Console.WriteLine($"  [{i}] URI: {uc.Uri} ({uc.MediaType})");
                break;

            default:
                Console.WriteLine($"  [{i}] {content.GetType().Name}: {content}");
                break;
        }
    }

    Console.WriteLine();
    Console.WriteLine("Done!");
});

return await rootCommand.InvokeAsync(args);
