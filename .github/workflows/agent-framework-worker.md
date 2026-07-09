---
name: "Agent Framework Template Worker"
description: >-
  Maintain a single draft pull request that bumps the aiagent-webapi project template's
  Microsoft.Agents.AI package versions in eng/packages/ProjectTemplates.props to the newest
  coherent Agent Framework release on the dotnet-public feed, CI-validated by restoring, building,
  and packing the template package through the repo's Arcade build and running its snapshot +
  execution tests. Invoked per-target by the Agent Framework Template Orchestrator (workflow_call)
  or manually (workflow_dispatch).

# Only run on a schedule for the canonical (non-fork) repository; allow manual dispatch anywhere.
if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

permissions:
  actions: read
  contents: read
  pull-requests: read
  issues: read

safe-outputs:
  # Threat detection screens the untrusted reviewer feedback this agent incorporates. It runs in a
  # separate gh-aw job that authenticates via the coalescing token below.
  threat-detection:
    engine:
      id: copilot
      env:
        # Workaround for github/gh-aw#43917: the detection job's `needs` omit `pat_pool`, so the
        # main engine's case(needs.pat_pool...) token can't resolve here. Authenticate by coalescing
        # the pool PAT secrets directly -- the first non-empty one wins.
        COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_PAT_0 || secrets.COPILOT_PAT_1 || secrets.COPILOT_PAT_2 || secrets.COPILOT_PAT_3 || secrets.COPILOT_PAT_4 || secrets.COPILOT_PAT_5 || secrets.COPILOT_PAT_6 || secrets.COPILOT_PAT_7 || secrets.COPILOT_PAT_8 || secrets.COPILOT_PAT_9 || secrets.COPILOT_GITHUB_TOKEN }}
  # The maintained PR is created, updated, and pushed to entirely through native gh-aw safe outputs
  # -- the agent job stays read-only and every write runs in a separate, permission-controlled job.
  # allowed-files restricts every write to the template's version file and template tree.
  create-pull-request:
    draft: true
    labels: [automation, area-ai-templates]
    base-branch: main
    preserve-branch-name: true
    if-no-changes: "warn"
    allowed-files:
      - "eng/packages/ProjectTemplates.props"
      - "src/ProjectTemplates/Microsoft.Agents.AI.ProjectTemplates/**"
  push-to-pull-request-branch:
    target: "*"
    required-labels: [automation, area-ai-templates]
    if-no-changes: "ignore"
    allowed-files:
      - "eng/packages/ProjectTemplates.props"
      - "src/ProjectTemplates/Microsoft.Agents.AI.ProjectTemplates/**"
  update-pull-request:
    target: "*"
    required-labels: [automation, area-ai-templates]
    title: true
    body: true
  add-comment:
    target: "*"
    required-labels: [automation, area-ai-templates]
    max: 1
  mark-pull-request-as-ready-for-review:
    target: "*"
    required-labels: [automation, area-ai-templates]
  noop:
    report-as-issue: false
  report-failure-as-issue: false

network:
  allowed:
    - defaults
    - dotnet

features:
  # Let a maintainer promote an external contributor's comment to "approved" by adding an
  # endorsement reaction (👍/❤️), so the agent then sees and can act on it. Requires the gh-proxy
  # mode (below) to attribute the reacting user.
  integrity-reactions: true

tools:
  github:
    mode: gh-proxy
    toolsets: [default]
    # Only content at "approved" integrity or higher reaches the agent. Write-access authors
    # (OWNER/MEMBER/COLLABORATOR) are approved automatically; feedback from anyone else is filtered
    # out unless a maintainer endorses it. This is the single source of truth for feedback trust.
    min-integrity: approved
  # Full bash: the agent applies the host-validated props file, runs git to position the PR branch,
  # and commits. The AWF sandbox + firewall, the read-only agent permissions, and the safe-outputs
  # allow-lists are the boundaries.
  bash: [":*"]

runs-on: ubuntu-latest
timeout-minutes: 180

# The agent commits the version bump and checks out the maintained PR branch for incremental
# appends. fetch: ["*"] pre-fetches every branch during the credentialed host checkout so the agent
# reaches the PR branch from an already-local ref.
checkout:
  fetch: ["*"]
  fetch-depth: 0

concurrency:
  group: agent-framework-worker
  cancel-in-progress: false

on:
  # Invoked per-target by the orchestrator as a reusable workflow so this runs in the orchestrator's
  # run context and inherits its actor, satisfying gh-aw activation's role check.
  workflow_call:
    inputs:
      target:
        description: "Framework release signal (JSON) from agent-framework-discover.cs."
        required: true
        type: string
  # Manual dispatch for standalone testing: provide a target JSON, or leave empty to have the setup
  # script run discovery itself.
  workflow_dispatch:
    inputs:
      target:
        description: "Framework release signal (JSON). Leave empty to resolve from the feed."
        required: false
        type: string
  permissions: {}

# Before the agent runs, deterministically prepare the run: resolve the target Agent Framework
# release, map it onto the template's package subset, CI-validate the prospective bump with a real
# restore + build, discover and classify the maintained draft PR, and compute the recommended
# lifecycle action. The agent consumes target.json instead of discovering or building anything.
steps:
  - name: Setup .NET
    uses: actions/setup-dotnet@9a946fdbd5fb07b82b2f5a4466058b876ab72bb2 # v5.3.0
    with:
      dotnet-version: "10.0.x"
  - name: Set up the run context and CI-validate the bump
    env:
      GH_TOKEN: ${{ github.token }}
      TARGET_JSON: ${{ inputs.target }}
    run: bash .github/scripts/agent-framework-worker-setup.sh

# After the agent runs, verify each full-body pull-request write still carries the tracking block the
# next run reads back to resume: the state block markers and a non-empty feedback-processed-through
# watermark. A body missing them would leave the next run unable to recover its place.
post-steps:
  - name: Validate agent output identity
    run: |
      set -euo pipefail
      out=/tmp/gh-aw/agent_output.json
      if [ ! -f "$out" ]; then
        echo "::notice::No agent output to validate"; exit 0
      fi
      idx=$(jq -r '(.items // []) | to_entries[]
        | select(.value.type=="create_pull_request"
            or (.value.type=="update_pull_request" and ((.value.operation // "replace")=="replace")))
        | select((.value.body // "") != "")
        | .key' "$out" 2>/dev/null || true)
      if [ -z "$idx" ]; then
        echo "::notice::No full-body PR-writing items -- nothing to validate"; exit 0
      fi
      rc=0
      while IFS= read -r i; do
        [ -n "$i" ] || continue
        typ=$(jq -r ".items[$i].type" "$out")
        body=$(jq -r ".items[$i].body" "$out")
        miss=""
        printf '%s' "$body" | grep -q '# agent-framework-template:state:begin' || miss="$miss begin-marker"
        printf '%s' "$body" | grep -q '# agent-framework-template:state:end'   || miss="$miss end-marker"
        printf '%s' "$body" | grep -Eq 'feedback-processed-through:[[:space:]]*[0-9]' || miss="$miss feedback-processed-through"
        if [ -n "$miss" ]; then
          echo "::error::$typ item #$i is missing required tracking identity:$miss"
          rc=1
        fi
      done <<< "$idx"
      if [ "$rc" -ne 0 ]; then
        # The generated safe-output job intentionally runs after failed agents to report
        # valid partial outputs. Quarantine this invalid mutation so it cannot be published.
        cp "$out" /tmp/gh-aw/agent/rejected_agent_output.json
        printf '%s\n' '{"items":[]}' > "$out"
        echo "::error::Rejected invalid agent output; downstream safe outputs will receive no items."
        exit "$rc"
      fi
      echo "Agent output identity validated."

# ###############################################################
# Select a PAT from the pool and override COPILOT_GITHUB_TOKEN.
# Run agentic jobs in an isolated `copilot-pat-pool` environment.
#
# When org-level billing is available, this will be removed.
# See `shared/pat_pool.README.md` for more information.
# ###############################################################
imports:
  - uses: shared/pat_pool.md
    with:
      environment: copilot-pat-pool

environment: copilot-pat-pool

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}
---

# Agent Framework Template Worker

## Goal

Keep the `aiagent-webapi` project template aligned with the newest coherent Microsoft Agent
Framework release by continuously maintaining a **single draft pull request** against `main` that
bumps the Agent Framework package versions in `eng/packages/ProjectTemplates.props`. Each run
consumes the host-prepared `target.json`, positions and updates that one PR, and leaves it in a
correct, idempotent state. The automation **never merges** -- humans review and merge.

The repository skill `update-agent-framework-template` (in `.github/skills/`) is the authority for
**how** to map the release signal onto the template's packages and format the PR. This workflow
governs **lifecycle/idempotency** (which PR to touch and what state to leave it in).

## Step 0 -- Read the run context (authoritative)

Read `/tmp/gh-aw/agent/target.json` (written by the setup script -- do **not** re-discover or
re-build). Key fields:

- `release_version`, `release_date`, `source_feed` -- the target Agent Framework release.
- `desired_versions` -- the exact `{ packageId: version }` the template must pin (already mapped to
  the template's subset; OpenAI tracks the core version).
- `current_version`, `main_needs_bump` -- what `main` pins today and whether it differs.
- `validated`, `build_summary` -- whether the prospective bump built through the repo's Arcade build:
  restore + build + **pack** of the `Microsoft.Agents.AI.ProjectTemplates` package (the `.nupkg` the
  execution tests install). The exact validated props content is at
  `/tmp/gh-aw/agent/ProjectTemplates.props.bumped` and the bumped template package project at
  `/tmp/gh-aw/agent/ProjectTemplates.csproj.bumped`.
- `pr`, `pr_state`, `pr_is_draft`, `pr_branch` -- the maintained PR (if any).
- `pr_recorded_version` -- the release the maintained PR already bumped to (from its tracking block).
- `classification` -- `ours` / `none` (see below).
- `has_new_feedback`, `watermark`, `run_started_at` -- review-activity wake gate and watermarks.
- `action` -- the recommended lifecycle action (`produce` or `noop`).
- `desired_branch` (`update-agent-framework-template`), `base_branch` (`main`), `props_path`.

Classifications:
- **`ours`** -- an open `automation`+`area-ai-templates` PR on `pr_branch` carrying the
  `agent-framework-template:state` block. Maintain it.
- **`none`** -- no open maintained PR.

## Step 1 -- Guard rails

- If `action` is `noop`: emit a `noop` safe output. Write the reason to the step summary. Do nothing
  else.
- If `validated` is `false`: the prospective bump did **not** build. **Never** open or update a PR
  with unvalidated versions. If a maintained PR exists (`ours`), post a single `add-comment`
  noting the build failure (quote the `build_summary`) and stop; otherwise emit `noop`. Do not emit
  `create-pull-request`, `push-to-pull-request-branch`, or `update-pull-request`.

## Step 2 -- Choose the action

With `validated: true`, pick the path from `classification` and the PR state:

| classification | PR state | Action |
|---|---|---|
| `none` | -- | **Fresh PR** (Step 3a) |
| `ours` | behind (`pr_recorded_version` != `release_version`), **draft** | **Incremental update** (Step 3b) |
| `ours` | caught up (`pr_recorded_version` == `release_version`) | **No-op** (already handled by `action: noop`) |
| `ours` | behind, **non-draft** | **Advisory only** (Step 3d) |

## Step 3 -- Apply and publish

Apply the changes by copying the host-prepared files into place -- never hand-edit versions, so what
you publish is exactly what was CI-validated:

```bash
# The Agent Framework package versions (lockstep, all Microsoft.Agents.AI packages):
cp /tmp/gh-aw/agent/ProjectTemplates.props.bumped eng/packages/ProjectTemplates.props
# The template NuGet package's own version, aligned to the release (Major/Minor/Patch; label kept):
cp /tmp/gh-aw/agent/ProjectTemplates.csproj.bumped "$(jq -r '.template_pkg_proj' /tmp/gh-aw/agent/target.json)"
```

Stage both files together in every commit. `target.json.template_pkg_old` -> `template_pkg_new` is the
template package version change (e.g. `1.3.0-preview` -> `1.13.0-preview`); the prerelease label is
never changed by the automation.

### 3a. Fresh PR (`classification: none`)

The run is checked out on `main` (the base). To avoid advancing `main` or producing an empty patch,
**detach HEAD first**, then apply, commit, and create a branch ref without switching:

```bash
git checkout --detach
cp /tmp/gh-aw/agent/ProjectTemplates.props.bumped eng/packages/ProjectTemplates.props
cp /tmp/gh-aw/agent/ProjectTemplates.csproj.bumped "$(jq -r '.template_pkg_proj' /tmp/gh-aw/agent/target.json)"
git add eng/packages/ProjectTemplates.props "$(jq -r '.template_pkg_proj' /tmp/gh-aw/agent/target.json)"
git commit -m "Update Agent Framework to <release_version>"
```

Choose the branch name: use `desired_branch`. Check the remote first
(`git ls-remote --heads origin "$DESIRED_BRANCH"`); if it already exists, suffix with the run id
(`{desired_branch}_{GITHUB_RUN_ID}`) and note the deviation in the body. Create the ref **without
switching** (`git branch {branch} HEAD`), then emit **`create-pull-request`** with:
- branch `{branch}`, title `Update Agent Framework to <release_version>`,
- the body from Step 4 (including the tracking block), draft (configured by the safe output).

### 3b. Incremental update (behind, draft, `ours`)

The PR branch already exists on the remote. Fetch and check it out, apply, and **append** a commit
(never amend/rebase/reset/squash/force-push):

```bash
git fetch origin "$PR_BRANCH" && git checkout "$PR_BRANCH"
cp /tmp/gh-aw/agent/ProjectTemplates.props.bumped eng/packages/ProjectTemplates.props
cp /tmp/gh-aw/agent/ProjectTemplates.csproj.bumped "$(jq -r '.template_pkg_proj' /tmp/gh-aw/agent/target.json)"
git add eng/packages/ProjectTemplates.props "$(jq -r '.template_pkg_proj' /tmp/gh-aw/agent/target.json)"
git commit -m "Update Agent Framework to <release_version>"
```

Emit **`push-to-pull-request-branch`** (target the PR), **`update-pull-request`** (full-body replace
per Step 4), and one **`add-comment`** summarizing the
delta (old -> new versions).

### 3d. Advisory only (behind, non-draft, `ours`)

Do **not** implement. Post one **`add-comment`** noting that a newer Agent Framework release
(`release_version`) is available and that re-marking the PR as draft lets the next scheduled run
apply it. Do not update the body.

## Step 4 -- PR body and tracking block

Regenerate the **entire** body on every full-body write. Include, succinctly:

1. A one-line summary: bumping the Microsoft.Agents.AI packages to Agent Framework `release_version`.
2. A table of the package version changes (old -> new) for **every** entry in `desired_versions`
   (all Microsoft.Agents.AI packages move together; note each package keeps its own tier, so some
   rows are `-preview.*` or `-alpha.*`).
3. The **template package version** change: `template_pkg_old` -> `template_pkg_new` (the
   `Microsoft.Agents.AI.ProjectTemplates` package's own version, aligned to the release
   Major/Minor/Patch with its prerelease label unchanged).
4. **CI validation**: state that `eng/packages/ProjectTemplates.props` and the template package
   version were bumped and the `Microsoft.Agents.AI.ProjectTemplates` package restored, built, and
   packed successfully through the repo's Arcade build (quote `build_summary`), and that the
   snapshot + execution tests passed (quote `tests_summary`).
5. A note that the automation maintains this draft; a human reviews and merges.

End the body with the machine-readable tracking block as the very last thing, wrapped in a `yaml`
code fence. The fence lines are **required** -- without them the `#` marker lines render as Markdown
headings instead of code. Reproduce the fence and the block exactly as shown (opening ```` ```yaml ````,
the block, closing ```` ``` ````), substituting the values:

```yaml
# agent-framework-template:state:begin
source-feed: <source_feed>
agent-framework-version: <release_version>
agent-framework-release-date: <release_date>
feedback-processed-through: <run_started_at>
# agent-framework-template:state:end
```

The post-run identity check fails the run if a full-body PR write is missing these markers or the
`feedback-processed-through` watermark, so always include them.

### Mandatory safe-output body preflight

Before emitting **any** `create-pull-request` or `update-pull-request` safe output, first assemble
its complete replacement `body` value, including the tracking block above. Do not submit a summary,
an abbreviated body, or a reference to a local file: the exact final body string in the safe-output
item must contain all of the following:

- one complete `# agent-framework-template:state:begin` ...
  `# agent-framework-template:state:end` block;
- a non-empty `agent-framework-version` set to `target.json.release_version`; and
- a non-empty `feedback-processed-through` set to `target.json.run_started_at`.

Validate that final string before submitting the safe output (inspect the exact value you will put
in `body`, not an earlier draft). If any invariant is missing, rebuild the complete body and validate
it again; do **not** emit the `create-pull-request` or `update-pull-request` item. This workflow never
uses append or prepend body updates: every `update-pull-request` body is a full replacement and must
pass this preflight.

## Notes

- The automation keeps the PR a **draft**; `mark-pull-request-as-ready-for-review` is reserved for a
  future code-complete signal and is not emitted in the normal flow.
- Never touch files outside the allowed list. Never edit `.github/**`, `global.json`, `nuget.config`,
  `Directory.Packages.props`, or any solution file. If a build produced `bin/`/`obj/`, do not stage
  them.
- Settle contradictory feedback by recency (latest timestamp wins).
