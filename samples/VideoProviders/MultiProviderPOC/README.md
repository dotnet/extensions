# Multi-Provider Video Generation POC

Unified CLI that demonstrates `IVideoGenerator` across four providers — OpenAI (Sora), Google Veo, Runway, and Luma AI — using the same MEAI abstractions with first-class properties.

## Prerequisites

Set API keys for the providers you want to test:

| Provider | Environment Variable | Get a Key |
|----------|---------------------|-----------|
| OpenAI   | `OPENAI_API_KEY`    | [platform.openai.com/api-keys](https://platform.openai.com/api-keys) |
| Google Veo | `GOOGLE_API_KEY`  | [aistudio.google.com/apikey](https://aistudio.google.com/apikey) |
| Runway   | `RUNWAY_API_KEY`    | [dev.runwayml.com](https://dev.runwayml.com/) |
| Luma AI  | `LUMA_API_KEY`      | [lumalabs.ai/dream-machine/api/keys](https://lumalabs.ai/dream-machine/api/keys) |

## Quick Start

```bash
# Text-to-video with OpenAI
dotnet run -- generate --provider openai "A cat playing piano" --output cat.mp4

# Text-to-video with Google Veo + audio + negative prompt
dotnet run -- generate --provider veo "Birds on a lake" --audio --negative-prompt "people, cars" --output birds.mp4

# Text-to-video with Runway + seed for reproducibility
dotnet run -- generate --provider runway "A dancer spinning" --seed 42 --output dancer.mp4

# Text-to-video with Luma + aspect ratio
dotnet run -- generate --provider luma "Flowers blooming" --aspect-ratio 9:16 --output flowers.mp4

# Image-to-video (any provider)
dotnet run -- image-to-video --provider openai "The scene comes alive" --image photo.jpg --output scene.mp4

# Edit a video (OpenAI, Runway)
dotnet run -- edit --provider openai "Change to sunset colors" --video <video-id> --output edited.mp4

# Extend a video (OpenAI, Luma)
dotnet run -- extend --provider openai "The scene continues" --video <video-id> --output extended.mp4
```

## Demo Script

Run the automated demo that exercises each provider's supported features:

```powershell
# Auto-detect providers from environment variables
./demo-multi-provider.ps1

# Run specific providers
./demo-multi-provider.ps1 -Providers "openai,veo"

# With a reference image for image-to-video tests
./demo-multi-provider.ps1 -ReferenceImage myimage.png

# Reset state and start fresh
./demo-multi-provider.ps1 -Reset
```

## Feature Matrix

| Feature | OpenAI | Google Veo | Runway | Luma AI |
|---------|:------:|:----------:|:------:|:-------:|
| Text-to-video | ✅ | ✅ | ✅ | ✅ |
| Image-to-video | ✅ | ✅ | ✅ | ✅ |
| Video edit | ✅ | ❌ | ✅ | ❌ |
| Video extend | ✅ | ✅ | ❌ | ✅ |
| `AspectRatio` | via Size | ✅ | ✅ | ✅ |
| `Seed` | ❌ | ✅ | ✅ | ❌ |
| `GenerateAudio` | ❌ | ✅ | ❌ | ❌ |
| `NegativePrompt` | ❌ | ✅ | ❌ | ❌ |
