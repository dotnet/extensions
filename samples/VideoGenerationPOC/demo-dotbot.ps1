#!/usr/bin/env pwsh
# Video Generation POC — end-to-end DotNetBot scenario
#
# Prerequisites:
#   - OPENAI_API_KEY environment variable set
#   - Reference image at $ReferenceImage (or pass -ReferenceImage path)
#
# This script demonstrates:
#   1. Image-to-video generation from a reference image
#   2. Character upload from the generated clip
#   3. Two character-consistent generations (surfing + groceries)
#   4. Editing the surfing video (sunset palette shift)
#   5. Extending the grocery video (fruit juggling)
#
# Resume: The script saves progress to a state file in the output directory.
#         If a step already completed (output file + ID exist), it is skipped.
#         Pass -Reset to start fresh.

param(
    [string]$ReferenceImage = "my-dotnet-bot-mod.png",
    [string]$OutputDir      = "..\..\artifacts\demo-output",
    [string]$Model          = "sora-2",
    [switch]$Reset
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path $ReferenceImage)) {
    Write-Error "Reference image not found: $ReferenceImage"
    exit 1
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# ── State management ────────────────────────────────────────────────────
$stateFile = Join-Path $OutputDir "demo-state.json"

if ($Reset -and (Test-Path $stateFile)) {
    Remove-Item $stateFile -Force
    Write-Host "State file cleared." -ForegroundColor Yellow
}

function Get-State {
    if (Test-Path $stateFile) {
        return Get-Content $stateFile -Raw | ConvertFrom-Json -AsHashtable
    }
    return @{}
}

function Set-State {
    param([string]$Key, [string]$Value)
    $s = Get-State
    $s[$Key] = $Value
    $s | ConvertTo-Json | Set-Content $stateFile
}

# ── Tool helpers ────────────────────────────────────────────────────────
function Invoke-Tool {
    param([string]$Label, [string[]]$Arguments)
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Cyan
    Write-Host "  $Label" -ForegroundColor Cyan
    Write-Host ("=" * 70) -ForegroundColor Cyan
    Write-Host "> dotnet run --project $PSScriptRoot -- $($Arguments -join ' ')" -ForegroundColor DarkGray

    $output = & dotnet run --project $PSScriptRoot -- @Arguments 2>&1
    $output | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tool exited with code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    return ($output | Out-String)
}

function Extract-Id {
    param([string]$Output, [string]$Prefix)
    if ($Output -match "$Prefix\:\s*(\S+)") {
        return $Matches[1]
    }
    Write-Error "Could not find $Prefix in tool output."
    exit 1
}

function Skip-OrRun {
    param(
        [string]$StateKey,
        [string]$Label,
        [string]$OutputFile,
        [scriptblock]$Action
    )
    $state = Get-State
    if ($state.ContainsKey($StateKey) -and ((-not $OutputFile) -or (Test-Path $OutputFile))) {
        Write-Host ""
        Write-Host ("=" * 70) -ForegroundColor DarkGray
        Write-Host "  SKIP: $Label (already completed — $StateKey=$($state[$StateKey]))" -ForegroundColor DarkGray
        Write-Host ("=" * 70) -ForegroundColor DarkGray
        return $state[$StateKey]
    }
    $id = & $Action
    Set-State $StateKey $id
    return $id
}

# ─────────────────────────────────────────────────────────────────────────
# Step 1: Generate a 4-second intro clip from the reference image
# ─────────────────────────────────────────────────────────────────────────
$introPath = Join-Path $OutputDir "01_DotNetBot_intro.mp4"
$introId = Skip-OrRun "introId" "Step 1: Image-to-video — DotNetBot intro (4s)" $introPath {
    $out = Invoke-Tool "Step 1: Image-to-video — DotNetBot intro (4s)" @(
        "generate",
        "A smooth 360-degree tracking shot around a cute spherical robot called DotNetBot.  He has an spherical shape with antenna on his head that remains stationary, two arms and legs, and a belt with buckle that reads '.NET'. The camera orbits to show every side, then DotNetBot lifts his right arm to give the shaka hand sign extending thumb and pinky finger.",
        "--input", $ReferenceImage,
        "--model", $Model,
        "--duration", "4",
        "--output", $introPath
    )
    $id = Extract-Id $out "OPERATION_ID"
    Write-Host "  >> Intro video ID: $id" -ForegroundColor Green
    return $id
}

# ─────────────────────────────────────────────────────────────────────────
# Step 1b: Trim the intro clip to ≤4.0 s for character upload
#   OpenAI requires character reference videos to be between 2–4 seconds,
#   but generated clips may slightly overshoot. Use ffmpeg to hard-trim.
# ─────────────────────────────────────────────────────────────────────────
$trimmedPath = Join-Path $OutputDir "01b_DotNetBot_intro_trimmed.mp4"
if ((Test-Path $introPath) -and -not (Test-Path $trimmedPath)) {
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Cyan
    Write-Host "  Step 1b: Trimming intro clip to 3.9 s with ffmpeg (re-encode)" -ForegroundColor Cyan
    Write-Host ("=" * 70) -ForegroundColor Cyan
    & ffmpeg -y -i $introPath -t 3.9 $trimmedPath 2>&1 | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) {
        Write-Error "ffmpeg trim failed (exit code $LASTEXITCODE). Is ffmpeg installed?"
        exit 1
    }
    Write-Host "  >> Trimmed clip: $trimmedPath" -ForegroundColor Green
} elseif (Test-Path $trimmedPath) {
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor DarkGray
    Write-Host "  SKIP: Step 1b — trimmed clip already exists" -ForegroundColor DarkGray
    Write-Host ("=" * 70) -ForegroundColor DarkGray
}

# ─────────────────────────────────────────────────────────────────────────
# Step 2: Upload the trimmed intro clip as character "DotNetBot"
# ─────────────────────────────────────────────────────────────────────────
$charId = Skip-OrRun "charId" "Step 2: Upload character 'DotNetBot'" "" {
    $out = Invoke-Tool "Step 2: Upload character 'DotNetBot'" @(
        "upload-character", "DotNetBot",
        "--input", $trimmedPath,
        "--model", $Model
    )
    $id = Extract-Id $out "CHARACTER_ID"
    Write-Host "  >> Character ID: $id" -ForegroundColor Green
    return $id
}

# ─────────────────────────────────────────────────────────────────────────
# Step 3: Generate DotNetBot surfing (with character)
# ─────────────────────────────────────────────────────────────────────────
$surfPath = Join-Path $OutputDir "03_DotNetBot_surfing.mp4"
$surfId = Skip-OrRun "surfId" "Step 3: DotNetBot goes surfing" $surfPath {
    $out = Invoke-Tool "Step 3: DotNetBot goes surfing" @(
        "generate",
        "A cinematic wide shot of DotNetBot surfing a massive turquoise wave at golden hour. Water sprays around him as he crouches on the board, sun flare behind.",
        "--character", $charId,
        "--model", $Model,
        "--duration", "8",
        "--output", $surfPath
    )
    $id = Extract-Id $out "OPERATION_ID"
    Write-Host "  >> Surfing video ID: $id" -ForegroundColor Green
    return $id
}

# ─────────────────────────────────────────────────────────────────────────
# Step 4: Generate DotNetBot buying groceries (with character)
# ─────────────────────────────────────────────────────────────────────────
$groceryPath = Join-Path $OutputDir "04_DotNetBot_groceries.mp4"
$groceryId = Skip-OrRun "groceryId" "Step 4: DotNetBot buys groceries" $groceryPath {
    $out = Invoke-Tool "Step 4: DotNetBot buys groceries" @(
        "generate",
        "A medium shot of DotNetBot rolling through a colorful outdoor market, picking up oranges and tomatoes with his small arms and placing them in a basket.",
        "--character", $charId,
        "--model", $Model,
        "--duration", "8",
        "--output", $groceryPath
    )
    $id = Extract-Id $out "OPERATION_ID"
    Write-Host "  >> Grocery video ID: $id" -ForegroundColor Green
    return $id
}

# ─────────────────────────────────────────────────────────────────────────
# Step 5: Edit the surfing video — shift to sunset tones
# ─────────────────────────────────────────────────────────────────────────
$editPath = Join-Path $OutputDir "05_DotNetBot_surfing_sunset.mp4"
$editId = Skip-OrRun "editId" "Step 5: Edit surfing video — warm sunset palette" $editPath {
    $out = Invoke-Tool "Step 5: Edit surfing video — warm sunset palette" @(
        "edit",
        "Shift the entire color palette to warm sunset tones - deep oranges, soft pinks, and golden highlights. The water turns a deep amber and the sky glows.",
        "--video", $surfId,
        "--model", $Model,
        "--output", $editPath
    )
    $id = Extract-Id $out "OPERATION_ID"
    Write-Host "  >> Edit video ID: $id" -ForegroundColor Green
    return $id
}

# ─────────────────────────────────────────────────────────────────────────
# Step 6: Extend the grocery video — DotNetBot juggles fruit
# ─────────────────────────────────────────────────────────────────────────
$extendPath = Join-Path $OutputDir "06_DotNetBot_groceries_extended.mp4"
$extendId = Skip-OrRun "extendId" "Step 6: Extend grocery video — fruit juggling exit" $extendPath {
    $out = Invoke-Tool "Step 6: Extend grocery video — fruit juggling exit" @(
        "extend",
        "Continue the scene as DotNetBot leaves the market stall juggling three oranges, rolling away happily while vendors cheer in the background.",
        "--video", $groceryId,
        "--model", $Model,
        "--duration", "8",
        "--output", $extendPath
    )
    $id = Extract-Id $out "OPERATION_ID"
    Write-Host "  >> Extended video ID: $id" -ForegroundColor Green
    return $id
}

# ─────────────────────────────────────────────────────────────────────────
# Summary
# ─────────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "  All done! Output files:" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Get-ChildItem $OutputDir -Filter "*.mp4" | ForEach-Object {
    Write-Host "  $_" -ForegroundColor Green
}
Write-Host ""
Write-Host "  State: $stateFile" -ForegroundColor DarkGray
Write-Host "  (pass -Reset to start fresh)" -ForegroundColor DarkGray
