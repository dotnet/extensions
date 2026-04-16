# Luma AI (Dream Machine) Video Generation Sample

This sample demonstrates using the **Microsoft.Extensions.AI** `IVideoGenerator` abstraction with Luma AI's Dream Machine API (Ray 2 models).

## Getting Access

1. Go to [https://lumalabs.ai/dream-machine/api/keys](https://lumalabs.ai/dream-machine/api/keys)
2. Sign in or create a Luma account
3. Create an API key
4. Check billing at [https://lumalabs.ai/dream-machine/api/billing/overview](https://lumalabs.ai/dream-machine/api/billing/overview)

## Environment Setup

```bash
export LUMA_API_KEY="luma-xxxx"
```

## Models

| Model | ID | Notes |
|---|---|---|
| Ray 2 | `ray-2` | Full quality, supports 540p–4k |
| Ray 2 Flash | `ray-flash-2` | Faster, lower cost |

## Supported Operations

| Operation | MEAI Mapping | Notes |
|---|---|---|
| Text-to-video | `VideoOperationKind.Create`, no `StartFrame` | Basic prompt → video |
| Image-to-video (start frame) | `VideoOperationKind.Create` + `StartFrame` (image) | Image as first frame (`keyframes.frame0`) |
| Image-to-video (start+end frames) | `VideoOperationKind.Create` + `StartFrame` + `EndFrame` | Two images as keyframes (`frame0`+`frame1`) for interpolation |
| Extend video | `VideoOperationKind.Extend` + `SourceVideoId` | Extend using the generation ID of a completed video |
| Reverse extend | `AdditionalProperties` | Extend backwards — requires provider-specific keyframe manipulation |
| Video interpolation | `AdditionalProperties` | Interpolate between two generation IDs |

## Usage

```bash
# Text-to-video
dotnet run -- generate "A tiger walking through snow" --output tiger.mp4

# Image-to-video with start frame
dotnet run -- generate "The scene comes alive" --image start.jpg --output scene.mp4

# Start + end frame interpolation
dotnet run -- generate "Smooth transition" --image start.jpg --end-image end.jpg

# Extend a completed video
dotnet run -- extend "The tiger starts running" --video <generation-id> --output extended.mp4

# With options
dotnet run -- generate "A neon cityscape" --model ray-2 --resolution 1080p --aspect-ratio 16:9 --loop --duration 5s
```

## API Gaps / Limitations

- **Image URLs only**: Luma requires HTTPS URLs for `promptImage`, not data URIs. The sample sends data URIs but the API may reject them — callers may need to pre-upload to a CDN.
- **No direct edit**: There is no video editing endpoint; `VideoOperationKind.Edit` is mapped to keyframe continuation which is not true editing.
- **Reverse extend**: Requires setting `SourceVideoId` as `frame1` (not `frame0`). This requires provider-specific handling not captured by the current abstraction.
- **Concepts/camera motion**: Luma supports "concepts" (e.g., `dolly_zoom`) and camera motion keywords in prompts. These are prompt-level, no dedicated API field.
- **Callback URL**: Luma supports `callback_url` for push-based status updates — not part of the MEAI polling model.
- **Modify Video**: Luma has a separate `/modify-video` endpoint for video editing (not modeled here).
- **Reframe**: Luma supports video/image reframing to different aspect ratios — a unique feature.
- **Add Audio**: Luma has a separate endpoint to add audio to a completed generation.
