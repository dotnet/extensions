---
name: update-otel-genai-conventions
description: >-
  Analyze OpenTelemetry GenAI semantic-conventions changes (PRs, CHANGELOG
  snapshots, date ranges, or releases when they exist) and produce
  compensating change plans for dotnet/extensions. The conventions now live
  in their own repo at open-telemetry/semantic-conventions-genai and cover
  gen-ai, mcp, openai, anthropic, aws-bedrock, and azure-ai-inference areas;
  the previous home was open-telemetry/semantic-conventions under the
  area:gen-ai label. Use when asked to "update OTel conventions", "check
  semantic-conventions-genai", "plan gen-ai convention changes", "bump genai
  semconv version", review gen-ai/MCP/provider convention PRs, or when given
  a PR number/URL, CHANGELOG snapshot, date range, or release version from
  either repo. Also use for "update OpenTelemetry", "bump semconv version",
  or "what changed in semantic-conventions-genai".
agent: 'agent'
tools: ['github/*', 'sql']
---

# Update OTel Gen-AI Conventions

Analyze OpenTelemetry GenAI semantic-conventions changes — PRs, `CHANGELOG.md` snapshots, date ranges, or releases — primarily from [`open-telemetry/semantic-conventions-genai`](https://github.com/open-telemetry/semantic-conventions-genai), and produce compensating updates in `dotnet/extensions`. See the [Migration Note](#migration-note) below for the recent move from the core `semantic-conventions` repo and how the two repos relate.

## Migration Note

The OpenTelemetry GenAI semantic conventions recently moved from
[`open-telemetry/semantic-conventions`](https://github.com/open-telemetry/semantic-conventions)
(where they lived under the `area:gen-ai` label) to a dedicated repo:
[`open-telemetry/semantic-conventions-genai`](https://github.com/open-telemetry/semantic-conventions-genai).
The new repo also hosts `mcp`, `openai`, `anthropic`, `aws-bedrock`, and
`azure-ai-inference` conventions.

Implications for this skill:

- **Primary input source** is the new repo. The old repo is a **fallback**
  for catch-up audits, historical context, and any in-flight PR that
  started there before the migration.
- **No `area:gen-ai` label** exists in the new repo — every PR is in scope
  by definition. The label still applies for old-repo catch-up work.
- **No releases yet** in the new repo.
  <!-- TODO: remove the "no releases yet" framing once semantic-conventions-genai ships its first release -->
  The `CHANGELOG.md` `Unreleased` section is the most reliable "what's new"
  view; pin a snapshot via commit SHA / ref for reproducible audits.
- **GenAI version is now independent** of core semconv. The schema URL
  `https://opentelemetry.io/schemas/gen-ai/X.Y.Z` carries the gen-ai
  version; core semconv tracks its own separate version line and is
  declared as a dependency in the new repo's `versions.env`.
- **Spec URL `https://opentelemetry.io/docs/specs/semconv/gen-ai/` is
  unchanged** — the published page still resolves and renders the gen-ai
  spec from the new repo, so the `<see href>` in dotnet/extensions source
  files does not need to change.
- **PR numbering** is not interchangeable across repos. Always
  disambiguate with `open-telemetry/semantic-conventions#NNN` or
  `open-telemetry/semantic-conventions-genai#NNN` when there is any risk
  of collision.
- **Doc-comment wording** in dotnet/extensions source still reads
  "Semantic Conventions for Generative AI systems v1.XX". The next
  convention-update PR should migrate this to "GenAI Semantic Conventions
  vX.Y.Z" — see [references/file-inventory.md §Version References](references/file-inventory.md#version-references).

When a referenced PR number doesn't resolve in the new repo, check the
old repo before assuming the input is invalid.

## Cross-repo applicability

This skill lives in `dotnet/extensions` and its file paths, build
commands, and PR-description conventions are tuned for that repo. The
new conventions repo also hosts provider-specific areas (`anthropic`,
`aws-bedrock`) whose dotnet instrumentation lives in **other** SDK
repositories that we contribute to:

| Upstream area | Repository | Notes |
|---|---|---|
| `anthropic` | [`anthropics/anthropic-sdk-csharp`](https://github.com/anthropics/anthropic-sdk-csharp) | Anthropic's official .NET SDK. |
| `aws-bedrock` | [`aws/aws-sdk-net`](https://github.com/aws/aws-sdk-net) | AWS Bedrock instrumentation lives in the `BedrockRuntime` service library (`AWSSDK.BedrockRuntime`) inside the AWS SDK monorepo. |

The skill can optionally be applied in those repos with the following
adaptations:

- **Apply** the convention analysis, classification framework
  ([references/change-classification.md](references/change-classification.md)),
  audit-table shape, area routing, doc-comment wording target
  ("GenAI Semantic Conventions vX.Y.Z"), version-reference grep
  recipes, and PR-description shape
  ([references/pr-description.md](references/pr-description.md)).
- **Do not assume** dotnet/extensions-specific paths
  (`src/Libraries/Microsoft.Extensions.AI*/`), the
  `OpenTelemetryConsts.cs` constants layout, the API-baseline workflow,
  or the build/test commands in
  [references/build-commands.md](references/build-commands.md). Use the
  target repo's own conventions for code structure, constants
  organization, and validation.
- **Scope by repo**: when running in another repo, the in-scope upstream
  area is the one that repo instruments (e.g. `anthropic` in
  `anthropics/anthropic-sdk-csharp`, `aws-bedrock` in the
  `BedrockRuntime` library of `aws/aws-sdk-net`). Other areas are out
  of scope from that repo's perspective even though they remain in
  scope for `dotnet/extensions`.
- **Pre-flight** still applies — search open PRs in the target repo for
  prior coverage before producing a plan.

## Mode Detection

Determine the operating mode from the user's request:

| Signal | Mode |
|--------|------|
| User asks to "audit" current implementation or "check alignment" with conventions | **Mode 1: Audit** |
| User asks to "update for vX.Y" or "apply vX.Y changes" in autopilot / one-shot | **Mode 2: Autopilot** |
| User asks to "generate a prompt" or "delegate to Copilot" or "CCA prompt" | **Mode 3: CCA Prompt** |
| Running inside Copilot Coding Agent with a prompt referencing this skill | **Mode 4: CCA Implementation** |
| User is in `/plan` mode, asks to "plan" changes, or asks to "implement" / "apply" changes | **Mode 5: Plan-then-Implement** |
| User asks to `/review` or "review" convention changes | **Mode 6: Review** |

If unclear, default to **Mode 5** (Plan-then-Implement) and offer Mode 3 as an alternative.

## Input Handling

The new repo (`open-telemetry/semantic-conventions-genai`) does not yet
publish releases. Until it does, the user typically provides one of:

- **PR references** in the new repo — full URL, `#NNN`, or
  `open-telemetry/semantic-conventions-genai#NNN` form. (No `area:` label
  filter is needed: the new repo is gen-ai-focused by definition.)
- **A `CHANGELOG.md` snapshot** — a commit SHA, branch ref, or a
  `https://github.com/open-telemetry/semantic-conventions-genai/blob/{ref}/CHANGELOG.md`
  URL pinning the `Unreleased` section at a point in time.
- **A date range or "since last update"** — list of PRs merged to the
  new repo's `main` between two refs / dates.
- **A release version or release URL** — once releases exist
  (`https://github.com/open-telemetry/semantic-conventions-genai/releases/tag/{version}`).

For **catch-up or historical work**, the old repo is still valid input:

- A **semantic-conventions release version** (e.g. `v1.40.0`) → fetch from
  `https://github.com/open-telemetry/semantic-conventions/releases/tag/{version}`
  (filter to `area:gen-ai` PRs).
- A **release URL** from the old repo → fetch the release notes directly.
- **PR references** from the old repo — only the ones with `area:gen-ai`.
  Use the `open-telemetry/semantic-conventions#NNN` form to disambiguate
  from new-repo PR numbers.

When PR numbers are given without a full URL, default to the **new** repo
and fall back to the **old** repo only if the PR doesn't exist in the new
repo or the user explicitly references the old repo.

### In-scope areas

The new repo hosts conventions for several areas, all of which this skill
covers (with placement guidance in
[references/implementation-patterns.md](references/implementation-patterns.md)):

| Upstream area (`model/<area>/`) | Maps to in dotnet/extensions |
|---|---|
| `gen-ai`, `gen-ai/agent` | `Microsoft.Extensions.AI` core (e.g. `OpenTelemetryChatClient`) |
| `mcp` | Currently no instrumentation; forward-looking — flag as a watch-list item if changes appear |
| `openai` | `Microsoft.Extensions.AI.OpenAI` |
| `anthropic` | Out of scope for `dotnet/extensions` today (no provider package). Implications land in [`anthropics/anthropic-sdk-csharp`](https://github.com/anthropics/anthropic-sdk-csharp) — apply this skill there per [Cross-repo applicability](#cross-repo-applicability). |
| `aws-bedrock` | Out of scope for `dotnet/extensions` today (no provider package). Implications land in the `BedrockRuntime` service library of [`aws/aws-sdk-net`](https://github.com/aws/aws-sdk-net) — apply this skill there per [Cross-repo applicability](#cross-repo-applicability). |
| `azure-ai-inference` | Corresponding provider package, if/when one exists in this repo; otherwise out of scope |

When classifying a change, identify its area from the path under
`model/<area>/` in the new repo (or from the doc page under
`docs/<area>/`).

### Existing dotnet/extensions PR Preflight

For **Mode 1: Audit** and **Mode 5: Plan-then-Implement**, after resolving the requested input identifiers but before doing deeper analysis or creating a plan, search open pull requests in `dotnet/extensions` to determine whether another PR already appears to cover the requested update.

Search using the requested release version, CHANGELOG ref, date range, or upstream PR numbers, plus relevant terms such as `gen-ai`, `GenAI`, `semantic conventions`, `semantic-conventions-genai`, `semconv-genai`, `OpenTelemetry`, `OTel`, and any in-scope area name (`MCP`, `OpenAI`, `Anthropic`, `Bedrock`, `Azure AI Inference`) that matches the changes you're working from. If one or more likely matching PRs are open, report the PR number, title, author, URL, and the signal that matched. Then stop and state that the audit or plan is not proceeding because an open PR already appears to cover the update.

Do not silently ignore search failures. If GitHub search/listing is unavailable, report the problem and ask the user whether to proceed without the preflight.

### Analyzing the Release / PRs

1. **Fetch the release notes** or PR descriptions and identify all gen-ai changes
2. **Read** [references/file-inventory.md](references/file-inventory.md) to understand which files in this repo are affected
3. **Classify each change** using [references/change-classification.md](references/change-classification.md)
4. **Check current state** — read the current source files to determine what is already implemented vs. what needs new work
5. **Build a changes audit table** showing each semantic convention change, its classification, and required action

For Step 4, read the source files listed in [references/file-inventory.md](references/file-inventory.md) (`OpenTelemetryConsts.cs`, `OpenTelemetryChatClient.cs`, `OpenTelemetryEmbeddingGenerator.cs`, `Common/FunctionInvocationProcessor.cs`, and any other OpenTelemetry* files).

### PR Title and Description Guidance

When creating or updating a PR after implementing GenAI semantic-conventions changes (from either repo), follow [references/pr-description.md](references/pr-description.md) for the title format and the changes-table shape.

---

## Mode 1: Audit

Audit the current gen-ai semantic conventions implementation against the latest published conventions to identify gaps, inconsistencies, or missed updates. Produces a plan that can be implemented locally (Mode 5) or delegated to CCA (Mode 3).

1. Complete the **Existing dotnet/extensions PR Preflight** above. If a matching open PR exists, report it and stop.
2. **Determine the current implemented version**: Read the version reference from `OpenTelemetryChatClient.cs` doc comment to identify which convention version the codebase claims to implement
3. **Check for version drift**: Verify every file with a gen-ai semantic conventions version reference uses the same version. Use the search command from [references/file-inventory.md](references/file-inventory.md#version-references). If files reference different versions, flag that as a critical gap requiring investigation.
4. **Fetch the latest convention spec**: Read the current conventions from the [published spec](https://opentelemetry.io/docs/specs/semconv/gen-ai/) (rendered from the new repo) and cross-check against the source of truth in [`open-telemetry/semantic-conventions-genai`](https://github.com/open-telemetry/semantic-conventions-genai) — `docs/<area>/` for human-readable docs, `model/<area>/` for the YAML registry, and `schema-snapshot/` plus `README.md` for the current schema URL (e.g. `opentelemetry.io/schemas/gen-ai/X.Y.Z`). The new repo's gen-ai version is independent of core semconv, so verify both. Until releases exist in the new repo, use the latest `Unreleased` `CHANGELOG.md` entries or recently merged PRs as the "latest release notes" equivalent.
5. **Read all current source files** listed in [references/file-inventory.md](references/file-inventory.md) to understand what is actually implemented
6. **Cross-reference**: For each attribute, metric, event, and operation name defined in the conventions:
   - Is the constant defined in `OpenTelemetryConsts.cs`?
   - Is it emitted in the relevant OpenTelemetry* client(s)?
   - Are version references consistent across all files?
   - Are tests covering the attribute/metric?
7. **Build an audit report** as a table:

   | Convention Item | Expected | Implemented | Gap |
   |----------------|----------|-------------|-----|
   | `gen_ai.request.attribute` | v1.XX | ✅ Yes / ❌ No / ⚠️ Partial | Description of gap |

8. **Produce a remediation plan** covering all identified gaps — formatted as either:
   - A **local plan** (Mode 5 format), or
   - A **CCA prompt** (Mode 3 format) suitable for delegation
   
   Ask the user which format they prefer, or produce both if requested.
9. **Verify this skill is still accurate** (same as Mode 6, step 6): compare skill content against the current codebase and call out any discrepancies

---

## Implementation Procedure

Modes 2, 4, and 5 share the same implementation flow. See [references/implementation-procedure.md](references/implementation-procedure.md).

---

## Mode 2: Autopilot

One-shot mode that analyzes the upstream input (release, PRs, CHANGELOG snapshot, or date range) and implements all changes in a single pass without intermediate review. Best for end-to-end execution when the user does not need a plan checkpoint.

1. Complete the **Input Handling** analysis above
2. Build an internal work plan in working memory (do not write `plan.md`):
   - Changes audit table with classification for each gen-ai change
   - Ordered list of implementation steps
3. Follow the **Implementation Procedure** above
4. Present a summary of all changes with the audit table showing what was implemented

---

## Mode 3: Generate CCA Prompt

Generate a structured prompt suitable for delegating to Copilot Coding Agent on github.com.

1. Complete the **Input Handling** analysis above
2. Read [references/prompt-template.md](references/prompt-template.md) for the template structure
3. Generate the prompt following the template, filling in:
   - Background with links to the upstream input (release URL, CHANGELOG snapshot ref, date range, or PR URLs)
   - Changes audit table (with **Area** column)
   - Required changes with exact file paths and code context from the current source
   - Test expectations referencing [references/testing-guide.md](references/testing-guide.md)
   - Validation steps
4. Present the prompt to the user for review

The generated prompt should reference this skill:
> Reference the `update-otel-genai-conventions` skill in `.github/skills/` for implementation patterns and testing guidance.

---

## Mode 4: CCA Implementation

When running inside Copilot Coding Agent (github.com) with a prompt that references this skill.

1. Parse the prompt to identify the required changes
2. Follow the **Implementation Procedure** above

---

## Mode 5: Plan-then-Implement

Generate a plan and (after user review/approval) implement it. Best when the user wants a checkpoint between analysis and execution. The runtime decides how to track work items (e.g., a task list, an in-memory queue, or a SQL `todos` table — whichever the agent already uses).

**Phase A: Plan** —

1. Resolve the user's input to one of: a release, PR identifiers, a `CHANGELOG.md` snapshot (commit SHA / ref), or a date range in the new repo (`open-telemetry/semantic-conventions-genai`). For catch-up work, accept upstream PRs from the old `open-telemetry/semantic-conventions` repo with `area:gen-ai`
2. Complete the **Existing dotnet/extensions PR Preflight** above. If a matching open PR exists, report it and stop without creating a plan.
3. Complete the **Analyzing the Release / PRs** analysis above
4. Create `plan.md` with a problem statement linking to the upstream input (release URL, CHANGELOG snapshot ref, date range, or list of PR URLs — whichever applies), a changes audit table, and a numbered list of work items. Each work item should call out the file(s) to modify, what code/constants/attributes to add, and which tests to update.
5. Pause for user review/approval before proceeding to Phase B

**Phase B: Implement** —

6. Read the existing `plan.md`
7. Follow the **Implementation Procedure** above for each work item

---

## Mode 6: Review

Review changes to gen-ai conventions against past patterns and known gotchas.

1. Identify the changes to review (local diff or PR diff)
2. Read [references/review-checklist.md](references/review-checklist.md) for the full checklist
3. Read [references/historical-releases.md](references/historical-releases.md) for past PR patterns. This file is point-in-time reference data from skill creation and may not include recent releases.
4. Check each item against the checklist:
   - Sensitive data gating (`EnableSensitiveData`)
   - Fluent Activity API chain style
   - Code deduplication (shared `Common/` classes)
   - Test augmentation vs. new tests
   - Version reference completeness
   - Exception recording approach (ILogger vs Activity.AddEvent)
5. Report findings with references to past PRs where similar feedback was given
6. **Verify this skill is still accurate**: Compare SKILL.md and all reference files against the current codebase (the codebase may have evolved — files moved, patterns changed). Recommend updates only for durable, cross-release guidance: workflow steps, validation commands, repository conventions, stable implementation patterns, file paths, test infrastructure. Do **not** pollute skill files with release-specific findings (per-version audits, one-off attribute mappings, etc.) — capture those in the review report, PR description, or implementation summary instead. Update `historical-releases.md` only when explicitly asked.

---

## Gotchas

Critical knowledge from past PR reviews that should inform all modes:

- **Exception recording**: Use `ILogger` with `[LoggerMessage]`, NOT `Activity.AddEvent`. The OTel SDK handles `Exception` passed to `ILogger`. See `OpenTelemetryLog.cs` in `Common/`.
- **Sensitive data**: Attributes that could contain user data (e.g. `exception.message`, message content) must be gated behind `EnableSensitiveData`. When in doubt, gate it.
- **Fluent chains**: Use fluent Activity API chains (`.SetStatus(...).SetTag(...)`) rather than separate statements.
- **Shared code**: Cross-cutting concerns (like exception logging) shared across multiple OpenTelemetry* clients belong in `src/Libraries/Microsoft.Extensions.AI/Common/`. Before adding a new helper, method, or internal type, search `Common/`, `TelemetryHelpers.cs`, `OpenTelemetryLog.cs`, and sibling OpenTelemetry* clients for existing logic with the same purpose — reuse or extend instead of introducing a parallel implementation. When the same helper is needed in 2+ places, factor it into `Common/` from the start. The same applies to parallel internal types: if a sibling client already defines a type with the same shape (same properties, same role, e.g. `RealtimeOtelFunction` vs `OtelFunction`), unify them under a single shared definition rather than letting each client carry its own copy.
- **Test augmentation**: Prefer augmenting existing test assertions over creating new test methods. Check for existing tests that validate the same scenario.
- **Version references**: When bumping the convention version, update all files that match the transitional regex `grep -rEn "Semantic Conventions for Generative AI systems v|GenAI Semantic Conventions v" src/Libraries/Microsoft.Extensions.AI/` (handles both pre- and post-migration wording). The next convention update should also migrate the wording in lockstep — see [references/file-inventory.md §Version References](references/file-inventory.md#version-references). Not all OpenTelemetry* files contain this reference — only update the ones that do.
- **No CHANGELOGs**: This repository no longer maintains per-library CHANGELOG.md files. Do NOT create or update any CHANGELOG files.
- **Source-generated JSON**: Adding new OTel part types requires: (1) new inner class, (2) `[JsonSerializable]` registration on `OtelContext`, (3) switch case in `SerializeChatMessages()`.
- **LoggerMessage text**: When using `[LoggerMessage]`, the message text should match the OTel event name for console logger readability.
- **No orphan constants**: Never add a constant to `OpenTelemetryConsts.cs` unless the same PR also adds at least one emission site for it. If the convention defines an attribute that no current client populates, classify the change as 🟢 *Constant not yet emitted* and defer the constant — do not add it ahead of emission. Verify with `grep -rn NewConstantName src/Libraries/Microsoft.Extensions.AI/` before submitting.
- **Area-aware constants**: Pick the nested class in `OpenTelemetryConsts.cs` based on the upstream area: `GenAI.*` for `gen-ai/*`, `MCP.*` for `mcp/*`. Provider-specific attributes (`openai.*`, `anthropic.*`, `aws-bedrock.*`, `azure-ai-inference.*`) generally belong in the **provider package's** constants file, not in `Microsoft.Extensions.AI/OpenTelemetryConsts.cs`. See [references/implementation-patterns.md §Area placement guidance](references/implementation-patterns.md#area-placement-guidance).

## Validation

After implementing changes (Modes 2, 4, and 5):

1. **Restore, build, and test** using the commands in [references/build-commands.md](references/build-commands.md) — pick the form (Windows or Linux/macOS) that matches your environment. Always remove any stale `SDK.sln*` files first; they cause build errors when present alongside a newly-generated filtered solution.
2. Verify no new build warnings in `artifacts/log/Build.binlog`
3. If the public API surface changed, regenerate the API baselines per [references/build-commands.md](references/build-commands.md) — then **discard baseline updates for unrelated libraries** (only keep baselines for libraries changed as part of the convention update)
