# Google Veo (Gemini API) Video Generation Sample

This sample demonstrates using the **Microsoft.Extensions.AI** `IVideoGenerator` abstraction with Google's Veo models via the Gemini API.

## Getting Access

1. Go to [https://aistudio.google.com/apikey](https://aistudio.google.com/apikey)
2. Create a Gemini API key
3. Veo models may require specific Google Cloud billing or allowlist access
4. See [Google Veo docs](https://ai.google.dev/gemini-api/docs/video) for feature availability

## Environment Setup

```bash
export GOOGLE_API_KEY="AIza..."
```

## Models

| Model | ID | Features |
|---|---|---|
| Veo 3.1 | `veo-3.1-generate-preview` | Text/image-to-video, extension, refer images, interpolation, 720p-4k, audio |
| Veo 3.1 Fast | `veo-3.1-fast-preview` | Same features, faster generation, lower quality |
| Veo 3 | `veo-3` | Text-to-video with native audio, 720p-1080p |
| Veo 2 | `veo-2` | Text/image-to-video, 720p-4k |

## Supported Operations

| Operation | MEAI Mapping | Notes |
|---|---|---|
| Text-to-video | `VideoOperationKind.Create`, no `StartFrame` | Prompt-only generation |
| Image-to-video | `VideoOperationKind.Create` + `StartFrame` (image) | Image as starting reference |
| First+last frame interpolation | `StartFrame` + `EndFrame` | Generate video between two frames |
| Reference images (up to 3) | `ReferenceImages` | Style/subject transfer with `reference_type` |
| Video extension | `VideoOperationKind.Extend` | Extend up to 20 times (7s each, 720p only) |
| Multiple outputs | `VideoGenerationOptions.Count` | Generate 1-4 videos from one request |

## Usage

```bash
# Text-to-video
dotnet run -- generate "A cinematic drone shot of a coastline at sunset" --output sunset.mp4

# Image-to-video
dotnet run -- generate "The scene comes alive" --image photo.jpg --output scene.mp4

# First+last frame interpolation
dotnet run -- generate "Smooth transition between frames" --image first.jpg --last-frame last.jpg --output interp.mp4

# Reference images for style
dotnet run -- generate "A character walking" --ref-image style1.png --ref-image style2.png --ref-type style

# With audio (Veo 3+)
dotnet run -- generate "A thunderstorm over a city" --model veo-3 --audio --output storm.mp4

# High resolution, specific duration
dotnet run -- generate "A serene lake" --resolution 4k --duration 8 --output lake.mp4

# With negative prompt
dotnet run -- generate "A person walking" --negative-prompt "blurry, distorted" --person-generation allow_adult

# Multiple outputs
dotnet run -- generate "A sunset" --count 4 --output sunset.mp4
```

## API Gaps / Limitations

- **Reference images with typed purpose**: Veo supports `referenceImages` with `referenceType` ("REFERENCE_TYPE_STYLE" or "REFERENCE_TYPE_SUBJECT"), allowing up to 3 images for style/subject transfer. MEAI's `ReferenceImages` collection maps well to this but doesn't include the `referenceType` metadata — provider-specific `AdditionalProperties` can be used for that.
- **First/last frame interpolation**: Veo generates a video between two keyframe images. MEAI's `StartFrame` and `EndFrame` properties map directly to this.
- **Native audio generation**: Veo 3+ can generate synchronized audio with video. MEAI has no audio-related option.
- **Negative prompts**: Veo supports `negativePrompt` to exclude unwanted elements. Not part of the core MEAI options.
- **Resolution as named tier**: Veo uses `"720p"`, `"1080p"`, `"4k"` — not pixel dimensions. The `VideoSize` abstraction works but the mapping is lossy.
- **Aspect ratio as string**: Veo uses `"16:9"`, `"9:16"` etc. `VideoSize` can encode this but it's different from the ratio concept each provider uses.
- **Duration as string**: Veo requires `durationSeconds` as a string (`"4"`, `"6"`, `"8"`). The `TimeSpan Duration` maps fine but the valid values are model-specific.
- **Person generation policy**: Veo has `personGeneration` (`"dont_allow"`, `"allow_adult"`) — a safety control with no MEAI equivalent.
- **Seed**: Reproducibility parameter not part of core MEAI options.
- **Video extension**: Extension works by passing frames from previous videos. The Veo API requires using the Gemini Files API to upload the source video first, making `SourceVideoId` insufficient as a simple string ID.
- **Operation polling model**: Veo returns a Gemini LRO (Long Running Operation) with `operations.get()`. The `VideoGenerationOperation.UpdateAsync()` pattern maps well to this.
- **Multiple videos**: Veo can generate 1-4 videos per request via `numberOfVideos`. MEAI's `Count` option maps to this, but `GetContentsAsync` returns them all in one list.
