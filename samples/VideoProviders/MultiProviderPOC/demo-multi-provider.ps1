#!/usr/bin/env pwsh
# Multi-Provider Video Generation Demo
#
# Runs through relevant scenarios for each provider based on feature support.
# Set the environment variables for the providers you want to test.
#
# Environment variables:
#   OPENAI_API_KEY  — OpenAI Sora
#   GOOGLE_API_KEY  — Google Veo
#   RUNWAY_API_KEY  — Runway
#   LUMA_API_KEY    — Luma AI
#
# Usage:
#   ./demo-multi-provider.ps1                        # Run all configured providers
#   ./demo-multi-provider.ps1 -Providers openai,veo  # Run specific providers
#   ./demo-multi-provider.ps1 -Reset                 # Clear state and start fresh



param(
    [string]$Providers     = "",          # Comma-separated: openai,veo,runway,luma (empty = auto-detect)
    [string]$OutputDir     = "..\..\artifacts\multi-provider-output",
    [string]$ReferenceImage = "",         # Optional image for image-to-video tests
    [switch]$Reset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot

# ── State management ────────────────────────────────────────────────────
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$stateFile = Join-Path $OutputDir "demo-state.json"

if ($Reset -and (Test-Path $stateFile)) {
    Remove-Item $stateFile -Force
    Write-Host "State cleared." -ForegroundColor Yellow
}

function Get-State {
    if (Test-Path $stateFile) { return Get-Content $stateFile -Raw | ConvertFrom-Json -AsHashtable }
    return @{}
}

function Set-State([string]$Key, [string]$Value) {
    $s = Get-State; $s[$Key] = $Value
    $s | ConvertTo-Json | Set-Content $stateFile
}

# ── Detect available providers ──────────────────────────────────────────
$providerMap = @{
    openai = "OPENAI_API_KEY"
    veo    = "GOOGLE_API_KEY"
    runway = "RUNWAY_API_KEY"
    luma   = "LUMA_API_KEY"
}

if ($Providers -ne "") {
    $activeProviders = $Providers -split "," | ForEach-Object { $_.Trim().ToLower() }
} else {
    $activeProviders = @()
    foreach ($p in $providerMap.Keys) {
        $envVar = $providerMap[$p]
        if ([Environment]::GetEnvironmentVariable($envVar)) {
            $activeProviders += $p
        }
    }
}

if ($activeProviders.Count -eq 0) {
    Write-Error "No providers configured. Set at least one API key environment variable."
    exit 1
}

Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "  Multi-Provider Video Generation Demo" -ForegroundColor Cyan
Write-Host "  Active providers: $($activeProviders -join ', ')" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan

# ── Helpers ─────────────────────────────────────────────────────────────
function Invoke-Tool([string]$Label, [string[]]$Arguments) {
    Write-Host ""
    Write-Host ("─" * 70) -ForegroundColor Cyan
    Write-Host "  $Label" -ForegroundColor Cyan
    Write-Host ("─" * 70) -ForegroundColor Cyan
    Write-Host "> dotnet run --project $ProjectDir -- $($Arguments -join ' ')" -ForegroundColor DarkGray

    $output = & dotnet run --project $ProjectDir -- @Arguments 2>&1
    $output | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Tool exited with code $LASTEXITCODE"
        return ""
    }
    return ($output | Out-String)
}

function Extract-Id([string]$Output, [string]$Prefix) {
    if ($Output -match "$Prefix\:\s*(\S+)") { return $Matches[1] }
    return ""
}

function Skip-OrRun([string]$StateKey, [string]$Label, [string]$OutputFile, [scriptblock]$Action) {
    $state = Get-State
    if ($state.ContainsKey($StateKey) -and $state[$StateKey] -ne "" -and
        ((-not $OutputFile) -or (Test-Path $OutputFile))) {
        Write-Host ""
        Write-Host ("─" * 70) -ForegroundColor DarkGray
        Write-Host "  SKIP: $Label ($StateKey=$($state[$StateKey]))" -ForegroundColor DarkGray
        return $state[$StateKey]
    }
    $id = & $Action
    if ($id -ne "") { Set-State $StateKey $id }
    return $id
}

# ═══════════════════════════════════════════════════════════════════════════
# Provider-specific scenarios
# ═══════════════════════════════════════════════════════════════════════════

# ─── OpenAI (Sora) ─────────────────────────────────────────────────────
function Run-OpenAI {
    Write-Host ""
    Write-Host ("═" * 70) -ForegroundColor Green
    Write-Host "  OPENAI (Sora)" -ForegroundColor Green
    Write-Host "  Features: text-to-video, image-to-video, edit, extend, characters" -ForegroundColor Green
    Write-Host ("═" * 70) -ForegroundColor Green

    # 1. Text-to-video
    $t2vPath = Join-Path $OutputDir "openai_01_text2video.mp4"
    $t2vId = Skip-OrRun "openai_t2v" "OpenAI: Text-to-video" $t2vPath {
        $out = Invoke-Tool "OpenAI: Text-to-video (8s, 1280x720)" @(
            "generate", "--provider", "openai",
            "A smooth tracking shot through a neon-lit cyberpunk city at night. Rain reflects colorful lights on the wet streets.",
            "--duration", "8", "--width", "1280", "--height", "720",
            "--output", $t2vPath
        )
        return (Extract-Id $out "OPERATION_ID")
    }

    # 2. Image-to-video (if reference image provided)
    if ($ReferenceImage -ne "" -and (Test-Path $ReferenceImage)) {
        $i2vPath = Join-Path $OutputDir "openai_02_image2video.mp4"
        $i2vId = Skip-OrRun "openai_i2v" "OpenAI: Image-to-video" $i2vPath {
            $out = Invoke-Tool "OpenAI: Image-to-video from reference" @(
                "image-to-video", "--provider", "openai",
                "A cinematic slow-motion shot inspired by the image, camera slowly orbiting around the subject.",
                "--image", $ReferenceImage, "--duration", "4",
                "--output", $i2vPath
            )
            return (Extract-Id $out "OPERATION_ID")
        }
    }

    # 3. Edit (requires previous video)
    if ($t2vId -ne "") {
        $editPath = Join-Path $OutputDir "openai_03_edit.mp4"
        Skip-OrRun "openai_edit" "OpenAI: Edit video" $editPath {
            $out = Invoke-Tool "OpenAI: Edit — shift to warm sunset palette" @(
                "edit", "--provider", "openai",
                "Shift the entire color palette to warm golden sunset tones with soft amber highlights.",
                "--video", $t2vId,
                "--output", $editPath
            )
            return (Extract-Id $out "OPERATION_ID")
        } | Out-Null
    }

    # 4. Extend (requires previous video)
    if ($t2vId -ne "") {
        $extPath = Join-Path $OutputDir "openai_04_extend.mp4"
        Skip-OrRun "openai_extend" "OpenAI: Extend video" $extPath {
            $out = Invoke-Tool "OpenAI: Extend — continue the scene" @(
                "extend", "--provider", "openai",
                "The camera rises above the buildings to reveal a stunning panoramic view of the cyberpunk skyline.",
                "--video", $t2vId, "--duration", "8",
                "--output", $extPath
            )
            return (Extract-Id $out "OPERATION_ID")
        } | Out-Null
    }
}

# ─── Google Veo ────────────────────────────────────────────────────────
function Run-Veo {
    Write-Host ""
    Write-Host ("═" * 70) -ForegroundColor Green
    Write-Host "  GOOGLE VEO" -ForegroundColor Green
    Write-Host "  Features: text-to-video, image-to-video, audio, negative prompt, seed, aspect ratio" -ForegroundColor Green
    Write-Host ("═" * 70) -ForegroundColor Green

    # 1. Text-to-video with audio and negative prompt
    $t2vPath = Join-Path $OutputDir "veo_01_text2video_audio.mp4"
    Skip-OrRun "veo_t2v" "Veo: Text-to-video with audio" $t2vPath {
        $out = Invoke-Tool "Veo: Text-to-video + audio + negative prompt" @(
            "generate", "--provider", "veo",
            "A serene mountain lake at dawn, birds singing, gentle water ripples.",
            "--audio",
            "--negative-prompt", "people, buildings, cars, text, watermark",
            "--aspect-ratio", "16:9",
            "--duration", "8",
            "--output", $t2vPath
        )
        return (Extract-Id $out "OPERATION_ID")
    } | Out-Null

    # 2. Text-to-video with seed for reproducibility
    $seedPath = Join-Path $OutputDir "veo_02_seeded.mp4"
    Skip-OrRun "veo_seed" "Veo: Seeded generation" $seedPath {
        $out = Invoke-Tool "Veo: Text-to-video with seed=42" @(
            "generate", "--provider", "veo",
            "A colorful hot air balloon festival with dozens of balloons taking off at sunrise.",
            "--seed", "42",
            "--aspect-ratio", "9:16",
            "--duration", "6",
            "--output", $seedPath
        )
        return (Extract-Id $out "OPERATION_ID")
    } | Out-Null

    # 3. Image-to-video (if reference image provided)
    if ($ReferenceImage -ne "" -and (Test-Path $ReferenceImage)) {
        $i2vPath = Join-Path $OutputDir "veo_03_image2video.mp4"
        Skip-OrRun "veo_i2v" "Veo: Image-to-video" $i2vPath {
            $out = Invoke-Tool "Veo: Image-to-video with audio" @(
                "image-to-video", "--provider", "veo",
                "The scene in the image comes to life with natural movement and ambient sounds.",
                "--image", $ReferenceImage,
                "--audio", "--duration", "4",
                "--output", $i2vPath
            )
            return (Extract-Id $out "OPERATION_ID")
        } | Out-Null
    }
}

# ─── Runway ────────────────────────────────────────────────────────────
function Run-Runway {
    Write-Host ""
    Write-Host ("═" * 70) -ForegroundColor Green
    Write-Host "  RUNWAY" -ForegroundColor Green
    Write-Host "  Features: text-to-video, image-to-video, video-to-video, seed" -ForegroundColor Green
    Write-Host ("═" * 70) -ForegroundColor Green

    # 1. Text-to-video with seed
    $t2vPath = Join-Path $OutputDir "runway_01_text2video.mp4"
    Skip-OrRun "runway_t2v" "Runway: Text-to-video" $t2vPath {
        $out = Invoke-Tool "Runway: Text-to-video with seed" @(
            "generate", "--provider", "runway",
            "A graceful ballet dancer performing a spin in an empty theater, dramatic lighting.",
            "--seed", "12345",
            "--duration", "5",
            "--aspect-ratio", "16:9",
            "--output", $t2vPath
        )
        return (Extract-Id $out "OPERATION_ID")
    } | Out-Null

    # 2. Image-to-video (if reference image provided)
    if ($ReferenceImage -ne "" -and (Test-Path $ReferenceImage)) {
        $i2vPath = Join-Path $OutputDir "runway_02_image2video.mp4"
        Skip-OrRun "runway_i2v" "Runway: Image-to-video" $i2vPath {
            $out = Invoke-Tool "Runway: Image-to-video" @(
                "image-to-video", "--provider", "runway",
                "The image gradually transforms into a cinematic scene with camera movement.",
                "--image", $ReferenceImage,
                "--duration", "5",
                "--output", $i2vPath
            )
            return (Extract-Id $out "OPERATION_ID")
        } | Out-Null
    }
}

# ─── Luma AI ───────────────────────────────────────────────────────────
function Run-Luma {
    Write-Host ""
    Write-Host ("═" * 70) -ForegroundColor Green
    Write-Host "  LUMA AI (Dream Machine)" -ForegroundColor Green
    Write-Host "  Features: text-to-video, image-to-video, extend, aspect ratio, keyframes" -ForegroundColor Green
    Write-Host ("═" * 70) -ForegroundColor Green

    # 1. Text-to-video with aspect ratio
    $t2vPath = Join-Path $OutputDir "luma_01_text2video.mp4"
    $t2vId = Skip-OrRun "luma_t2v" "Luma: Text-to-video" $t2vPath {
        $out = Invoke-Tool "Luma: Text-to-video with 9:16 aspect ratio" @(
            "generate", "--provider", "luma",
            "A time-lapse of flowers blooming in a garden, petals unfurling in sunlight.",
            "--aspect-ratio", "9:16",
            "--output", $t2vPath
        )
        return (Extract-Id $out "OPERATION_ID")
    }

    # 2. Image-to-video (if reference image provided)
    if ($ReferenceImage -ne "" -and (Test-Path $ReferenceImage)) {
        $i2vPath = Join-Path $OutputDir "luma_02_image2video.mp4"
        Skip-OrRun "luma_i2v" "Luma: Image-to-video" $i2vPath {
            $out = Invoke-Tool "Luma: Image-to-video from keyframe" @(
                "image-to-video", "--provider", "luma",
                "The image comes to life — subjects begin to move naturally.",
                "--image", $ReferenceImage,
                "--output", $i2vPath
            )
            return (Extract-Id $out "OPERATION_ID")
        } | Out-Null
    }

    # 3. Extend (requires previous video)
    if ($t2vId -ne "") {
        $extPath = Join-Path $OutputDir "luma_03_extend.mp4"
        Skip-OrRun "luma_extend" "Luma: Extend video" $extPath {
            $out = Invoke-Tool "Luma: Extend — continue blooming scene" @(
                "extend", "--provider", "luma",
                "The garden continues to bloom as butterflies arrive and the sun moves across the sky.",
                "--video", $t2vId,
                "--output", $extPath
            )
            return (Extract-Id $out "OPERATION_ID")
        } | Out-Null
    }
}

# ═══════════════════════════════════════════════════════════════════════════
# Run scenarios for each active provider
# ═══════════════════════════════════════════════════════════════════════════
foreach ($p in $activeProviders) {
    switch ($p) {
        "openai" { Run-OpenAI }
        "veo"    { Run-Veo }
        "runway" { Run-Runway }
        "luma"   { Run-Luma }
        default  { Write-Warning "Unknown provider: $p" }
    }
}

# ── Summary ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host ("═" * 70) -ForegroundColor Cyan
Write-Host "  Demo complete! Output files:" -ForegroundColor Cyan
Write-Host ("═" * 70) -ForegroundColor Cyan
if (Test-Path $OutputDir) {
    Get-ChildItem $OutputDir -Filter "*.mp4" | ForEach-Object {
        Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)" -ForegroundColor Green
    }
}
Write-Host ""
Write-Host "  State: $stateFile" -ForegroundColor DarkGray
Write-Host "  (pass -Reset to start fresh)" -ForegroundColor DarkGray
