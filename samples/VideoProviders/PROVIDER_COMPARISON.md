# Video Generation Provider Comparison & MEAI API Gap Analysis

This document summarizes findings from implementing `IVideoGenerator` across four providers (OpenAI Sora, Google Veo, Runway, and Luma AI), identifies gaps in the current MEAI abstractions, and recommends potential API additions.

## Provider Feature Matrix

| Feature | OpenAI (Sora) | Google Veo 3.1 | Runway | Luma AI (Ray 2) |
|---|:---:|:---:|:---:|:---:|
| **Text-to-video** | ✅ | ✅ | ✅ | ✅ |
| **Image-to-video** | ✅ | ✅ | ✅ (i2v endpoint) | ✅ (keyframe) |
| **Video edit** | ✅ | ❌ | ✅ (v2v with gen4_aleph) | ❌¹ |
| **Video extend** | ✅ | ✅ (up to 20×) | ❌ | ✅ (fwd + reverse) |
| **Characters / Avatars** | ✅ (upload video) | ❌ | ✅ (act_two + avatars) | ❌ |
| **Reference images** | ❌ | ✅ (up to 3, typed) | ✅ (v2v references) | ❌ |
| **First+last frame interp** | ❌ | ✅ | ❌ | ✅ (frame0 + frame1) |
| **Native audio** | ❌ | ✅ (Veo 3+) | ❌² | ❌³ |
| **Negative prompt** | ❌ | ✅ | ❌ | ❌ |
| **Seed / reproducibility** | ❌ | ✅ | ✅ | ❌ |
| **Resolution control** | ✅ (WxH pixels) | ✅ (720p/1080p/4k) | ✅ (ratio string) | ✅ (540p–4k) |
| **Aspect ratio** | Implied via Size | ✅ (string) | ✅ (ratio string) | ✅ (string) |
| **Duration** | ✅ (string enum: 4/8/12) | ✅ (string enum: 4/6/8) | ✅ (integer: 2–10) | ✅ (string: "5s") |
| **Multiple outputs** | ❌ | ✅ (1–4) | ❌ | ❌ |
| **Looping video** | ❌ | ❌ | ❌ | ✅ |
| **Callback/webhook** | ❌ | ❌ | ❌ | ✅ |
| **Content moderation params** | ❌ | ✅ (personGeneration) | ✅ (publicFigureThreshold) | ❌ |

¹ Luma has a separate "Modify Video" endpoint not covered in this evaluation.
² Runway provides separate sound effect, TTS, and speech-to-speech endpoints.  
³ Luma has a separate "Add Audio" endpoint.

## Async Polling Patterns

All four providers use an async task/operation model that maps well to `VideoGenerationOperation`:

| Provider | Submit | Poll | ID Format |
|---|---|---|---|
| OpenAI | `POST /videos/generations` | `GET /videos/generations/{id}` | `vg_xxxxx` |
| Google Veo | `POST /models/{model}:generateVideos` | `GET /{operation.name}` | `operations/xxx` |
| Runway | `POST /v1/{type}_to_video` | `GET /v1/tasks/{id}` | UUID |
| Luma AI | `POST /dream-machine/v1/generations` | `GET /dream-machine/v1/generations/{id}` | UUID |

**Assessment**: The `VideoGenerationOperation` pattern (submit → poll → download) is well-suited for all providers. The polling interval varies (5s for Runway/Luma, 10s for Veo).

## Input Media Handling

| Provider | Image Input | Video Input | Data URI Support |
|---|---|---|---|
| OpenAI | Data URI in JSON body | Multipart upload | ✅ (images) |
| Google Veo | Base64 bytes in JSON | Gemini Files API | ✅ (inline base64) |
| Runway | HTTPS URL or data URI | HTTPS URL or data URI | ✅ |
| Luma AI | HTTPS URL only¹ | HTTPS URL only | ❌ |

¹ Luma documentation says HTTPS URLs; data URI support is undocumented.

**Assessment**: Most providers accept data URIs or inline base64, making `DataContent` a good abstraction. However, Luma's URL-only requirement means some providers will require an out-of-band upload step.

## Identified API Gaps

### Gap 1: Seed / Reproducibility (HIGH PRIORITY)

**Problem**: 3 of 4 providers support a `seed` parameter for reproducible generation. Currently this requires `AdditionalProperties["seed"]`.

**Recommendation**: Add `int? Seed` to `VideoGenerationOptions`.

```csharp
/// <summary>Seed for reproducible generation. Same seed + same parameters ≈ same output.</summary>
public int? Seed { get; set; }
```

**Providers**: Google Veo ✅, Runway ✅, Luma ❌, OpenAI ❌

---

### Gap 2: Aspect Ratio (HIGH PRIORITY)

**Problem**: Every provider has a concept of aspect ratio (`"16:9"`, `"9:16"`, `"1:1"`, etc.) separate from pixel resolution. The current `VideoSize` property encodes pixel dimensions, but ratio is the primary concept for most providers. Mapping `Size(1280, 720)` → `"16:9"` is lossy and ambiguous.

**Recommendation**: Add `string? AspectRatio` to `VideoGenerationOptions`.

```csharp
/// <summary>Aspect ratio of the generated video (e.g., "16:9", "9:16", "1:1").</summary>
public string? AspectRatio { get; set; }
```

**Providers**: Google Veo ✅, Runway ✅, Luma ✅, OpenAI (implicit via Size)

---

### Gap 3: Negative Prompt (MEDIUM PRIORITY)

**Problem**: Google Veo supports `negativePrompt` to exclude unwanted elements. This is a concept that exists broadly in image generation (Stable Diffusion, DALL-E) and may appear in more video providers.

**Recommendation**: Add `string? NegativePrompt` to `VideoGenerationRequest`.

```csharp
/// <summary>Describes what to avoid in the generated video.</summary>
public string? NegativePrompt { get; set; }
```

**Providers**: Google Veo ✅ (others may add support)

---

### Gap 4: Reference Images with Purpose (MEDIUM PRIORITY)

**Problem**: Google Veo supports up to 3 reference images, each with a `referenceType` ("STYLE" or "SUBJECT"). Runway's video-to-video supports reference images for style transfer.

**Status**: ✅ ADDRESSED — `VideoGenerationRequest.ReferenceImages` (IList<AIContent>?) provides a first-class collection for reference images. The `referenceType` metadata can be provided via provider-specific `AdditionalProperties` on the individual `AIContent` items.

**Providers**: Google Veo ✅ (3 refs, typed), Runway ✅ (1 ref for v2v)

---

### Gap 5: Audio Generation (LOW-MEDIUM PRIORITY)

**Problem**: Google Veo 3+ generates synchronized audio with video natively. Luma and Runway offer separate audio endpoints. As video AI evolves, audio-with-video will likely become standard.

**Recommendation**: Add `bool? GenerateAudio` to `VideoGenerationOptions`.

```csharp
/// <summary>Whether to generate synchronized audio alongside the video.</summary>
public bool? GenerateAudio { get; set; }
```

Alternatively, this could be modeled as part of `MediaType` (e.g., `"video/mp4; codecs=avc1,mp4a"`) but that's less ergonomic.

**Providers**: Google Veo ✅ (native), Luma ✅ (separate endpoint), Runway ✅ (separate endpoint)

---

### Gap 6: Keyframe / Interpolation (LOW PRIORITY)

**Problem**: Both Luma and Google Veo support first+last frame interpolation — providing a start and end image and generating the video in between.

**Status**: ✅ ADDRESSED — `VideoGenerationRequest.StartFrame` and `VideoGenerationRequest.EndFrame` provide first-class properties for first/last frame interpolation.

---

### Gap 7: Reverse Extend (LOW PRIORITY)

**Problem**: Luma supports extending a video backwards (generating content leading up to the existing video). This is conceptually different from forward extension.

**Recommendation**: Consider adding `ReverseExtend` to `VideoOperationKind`, or leave as `AdditionalProperties`.

---

### Gap 8: Looping Video (LOW PRIORITY)

**Problem**: Luma supports `loop: true` to generate seamlessly looping video.

**Recommendation**: Leave as `AdditionalProperties["loop"]` unless more providers add support.

---

### Gap 9: Content Moderation / Safety Parameters (LOW PRIORITY)

**Problem**: Both Runway (`publicFigureThreshold`) and Google Veo (`personGeneration`) have provider-specific content moderation controls. These are safety parameters rather than creative controls.

**Recommendation**: Leave as `AdditionalProperties` — these are inherently provider-specific policies.

---

## Problems Encountered During Implementation

### 1. Runway's Separate Endpoints

Runway uses three separate endpoints (`text_to_video`, `image_to_video`, `video_to_video`) rather than a single unified endpoint. The `IVideoGenerator.GenerateAsync` single-method approach requires the implementation to inspect `StartFrame`/`SourceVideo` properties to determine which endpoint to call. This works cleanly with the new typed properties.

### 2. Luma's URL-Only Image Input

Luma requires HTTPS URLs for images — it doesn't accept data URIs or inline base64. Implementations targeting Luma need an upload step before generation, which is outside the scope of `GenerateAsync`. The `UriContent` type helps, but `DataContent` users will need pre-upload.

### 3. Google Veo's Extension Model

Veo video extension requires uploading the source video through the Gemini Files API first, then referencing it. A simple `SourceVideoId` string is insufficient for the multi-step extension workflow. The extension operation also has limitations (720p only, 7s segments, up to 20 times).

### 4. Ratio vs Size Ambiguity

Every provider has a different approach to sizing:
- **OpenAI**: Width × Height pixels (e.g., 1280×720)
- **Google Veo**: Named resolution string + optional aspect ratio
- **Runway**: Fixed ratio strings (e.g., `"1280:720"`, `"1104:832"`)
- **Luma**: Named resolution (540p/720p/1080p/4k) + optional aspect ratio

The `Size VideoSize` property maps well to OpenAI but requires lossy conversion for others. Adding `AspectRatio` as a separate property would help significantly.

### 5. Duration Representation

All providers handle duration differently:
- **OpenAI**: String enum (`"4"`, `"8"`, `"12"`)
- **Google Veo**: String enum (`"4"`, `"6"`, `"8"`)
- **Runway**: Integer (2–10)
- **Luma**: String with unit (`"5s"`)

`TimeSpan Duration` is a good neutral abstraction, but the valid values are provider- and model-specific. Documentation should make clear that providers snap to supported values.

## Summary of Recommendations

| Priority | Recommendation | Rationale |
|---|---|---|
| **HIGH** | Add `string? AspectRatio` to `VideoGenerationOptions` | Universal concept across all providers, lossy via `VideoSize` alone |
| **HIGH** | Add `int? Seed` to `VideoGenerationOptions` | 3 of 4 providers support it, common for iterative creative workflows |
| **MEDIUM** | Add `string? NegativePrompt` to `VideoGenerationRequest` | Proven concept from image gen; Veo supports it, others likely will |
| **MEDIUM** | Add typed reference media concept | Veo + Runway use reference images with purpose; different from input media |
| **LOW-MED** | Add `bool? GenerateAudio` to `VideoGenerationOptions` | Growing trend for integrated audio; 3 providers offer it in some form |
| **LOW** | Consider `ReverseExtend` in `VideoOperationKind` | Luma-specific for now, but a useful concept for storytelling |

## What Works Well

- **`VideoGenerationOperation` pattern**: The submit → poll → download lifecycle maps perfectly to all four providers.
- **`VideoOperationKind` enum**: Create/Edit/Extend covers the core operations well.
- **`StartFrame`/`EndFrame`/`ReferenceImages` properties**: Handles image-to-video, interpolation, and reference images with clear semantics for all providers.
- **`AdditionalProperties` escape hatch**: Provider-specific features (concepts, camera motion, content moderation) flow through cleanly.
- **`GetService<T>()` pattern**: Enables provider-specific extensions (like OpenAI's `UploadVideoCharacterAsync`) without polluting the interface.
- **`VideoGenerationResponseFormat`**: Uri vs Data choice is useful for all providers.
- **`TimeSpan Duration`**: Clean neutral type that each provider maps to its own format.
