# Runway Video Generation Sample

This sample demonstrates using the **Microsoft.Extensions.AI** `IVideoGenerator` abstraction with Runway's generative video API.

## Getting Access

1. Go to [https://dev.runwayml.com/](https://dev.runwayml.com/) and sign in
2. Navigate to **API Keys** and create a new key
3. Runway requires a paid plan with credits — see [pricing](https://runwayml.com/pricing)
4. API version header `X-Runway-Version: 2024-11-06` is required on all requests

## Environment Setup

```bash
export RUNWAY_API_KEY="rw_xxxx"
```

## Models

| Model | ID | Capabilities | Credits/sec |
|---|---|---|---|
| Gen-4.5 | `gen4.5` | Text-to-video only | 12 |
| Gen-4 Turbo | `gen4_turbo` | Text-to-video, image-to-video | 4 |
| Gen-4 Aleph | `gen4_aleph` | Video-to-video only | 4 |
| Veo 3.1 | `veo3.1` | Text-to-video (via Runway) | 4 |
| Veo 3 | `veo3` | Text-to-video (via Runway) | 4 |

## Supported Operations

| Operation | MEAI Mapping | Endpoint |
|---|---|---|
| Text-to-video | `VideoOperationKind.Create`, no `StartFrame` | `POST /v1/text_to_video` |
| Image-to-video | `VideoOperationKind.Create` + `StartFrame` (image) | `POST /v1/image_to_video` |
| Video-to-video | `VideoOperationKind.Edit` + `SourceVideo` (video) | `POST /v1/video_to_video` |

## Usage

```bash
# Text-to-video
dotnet run -- text-to-video "A cute bunny hopping in a meadow" --output bunny.mp4

# Image-to-video
dotnet run -- image-to-video "The scene comes alive" --image bunny.jpg --duration 10 --output scene.mp4

# Video-to-video (gen4_aleph) with style reference
dotnet run -- video-to-video "Add easter elements" --video https://example.com/cats.mp4 --reference style.jpg --output styled.mp4

# With seed for reproducibility
dotnet run -- text-to-video "A sunset over mountains" --seed 42 --output sunset.mp4
```

## API Gaps / Limitations

- **No extend**: Runway does not have an endpoint for extending a completed video. `VideoOperationKind.Extend` cannot be mapped.
- **Separate endpoints**: Runway uses three separate endpoints (`text_to_video`, `image_to_video`, `video_to_video`) requiring the implementation to dispatch based on input media type, rather than a single unified endpoint.
- **Ratio vs Size**: Runway uses fixed ratio strings (`"1280:720"`, `"720:1280"`, etc.) rather than arbitrary pixel dimensions. The `VideoSize` → ratio mapping loses information.
- **Character performance** (`act_two`): Runway has a unique `character_performance` endpoint for driving a character with a reference video. This is fundamentally different from OpenAI's character system and has no MEAI equivalent.
- **Seed**: Available via `AdditionalProperties` — consider promoting to a first-class option.
- **Image position**: Runway's `image_to_video` accepts an array of `PromptImages` with `position` (currently only `"first"`). MEAI models this via `StartFrame` for the first frame.
- **Duration as integer**: Runway passes duration as an integer (2-10), while OpenAI requires a string enum. The MEAI `TimeSpan Duration` maps cleanly to both.
- **Video-to-video references**: `gen4_aleph` supports `references` (array of image references for style). These could be modeled via `ReferenceImages` on the request.
- **Content moderation**: Runway has `contentModeration.publicFigureThreshold` — provider-specific safety control.
- **No resolution control for v2v**: For video-to-video, the output resolution is determined by the input video.
