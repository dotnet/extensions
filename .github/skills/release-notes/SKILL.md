---
name: release-notes
description: 'Draft release notes for a dotnet/extensions release. Gathers merged PRs, assigns them to packages by file path, categorizes by area and impact, tracks experimental API changes, and produces formatted markdown suitable for a GitHub release. Handles both monthly full releases and targeted intra-month patch releases.'
agent: 'agent'
tools: ['github/*']
---

# Release Notes

Draft release notes for a `dotnet/extensions` release. This skill gathers merged PRs between two tags, maps them to affected packages by examining changed file paths, categorizes entries by area and impact, audits experimental API changes, and produces concise markdown suitable for a GitHub release.

> **User confirmation required: This skill NEVER publishes a GitHub release without explicit user confirmation.** The user must review and approve the draft before any release is created.

## Context

The `dotnet/extensions` repository ships NuGet packages across many functional areas (AI, HTTP Resilience, Diagnostics, Compliance, Telemetry, etc.). Releases come in two forms:

- **Monthly full releases** — all packages ship together with a minor version bump (e.g. v10.3.0 → v10.4.0)
- **Intra-month patch releases** — a targeted subset of packages ships with a patch version bump (e.g. v10.3.1), typically addressing specific bug fixes or urgent changes

The repository does not follow Semantic Versioning. Major versions align with annual .NET releases, minor versions increment monthly, and patch versions are for intra-month fixes.

The repository makes heavy use of `[Experimental]` attributes. Experimental diagnostic IDs are documented in [`docs/list-of-diagnostics.md`](../../docs/list-of-diagnostics.md). Breaking changes to experimental APIs are expected and acceptable. Graduation of experimental APIs to stable is a noteworthy positive event.

The repository uses `release/` branches (e.g. `release/10.4`) where release tags are associated with commits on those branches. When determining the commit range for a release, ensure the previous and target tags are resolved against the appropriate release branch history.

## Execution Guidelines

- **Do not write intermediate files to disk.** Use the **SQL tool** for structured storage and querying (see [references/sql-storage.md](references/sql-storage.md) for schema).
- **Do not run linters, formatters, or validators** on the output.
- **Maximize parallel tool calls.** Fetch multiple PR and issue details in a single response.
- **Package assignment is file-path-driven.** Determine which packages a PR affects by examining which `src/Libraries/{PackageName}/` paths it touches. See [references/package-areas.md](references/package-areas.md) for the mapping. Use `area-*` labels only as a fallback.

## Process

Work through each step sequentially. Present findings at each step and get user confirmation before proceeding.

### Step 1: Determine Release Scope

The user may provide:
- **Two tags** (previous and target) — use these directly
- **A target tag only** — determine the previous release from `gh release list --repo dotnet/extensions --exclude-drafts`
- **No context** — show the last 5 published releases and ask the user to select

Once the range is established:

1. Determine if this is a **full release** (minor version bump) or **patch release** (patch version bump) based on the version numbers.
2. For patch releases, ask the user which packages are included (or infer from the PRs).
3. Get the merge date range for PR collection.

### Step 2: Collect and Enrich PRs

Follow [references/collect-prs.md](references/collect-prs.md):

1. Search for merged PRs in the date range between the two tags.
2. For each PR, fetch the file list and assign packages based on `src/Libraries/{PackageName}/` paths.
3. Enrich with full PR body, reactions, linked issues, and co-author data.
4. Apply exclusion filters (backports, automated version bumps, etc.).
5. Mark remaining PRs as candidates.

Store all data using the SQL tool.

### Step 3: Categorize and Group

Follow [references/categorize-entries.md](references/categorize-entries.md):

1. **Assign categories**: What's Changed, Documentation Updates, Test Improvements, or Repository Infrastructure Updates.
2. **Group by package area**: For "What's Changed" entries, group under descriptive area headings from [references/package-areas.md](references/package-areas.md). Each area heading must clearly identify the packages it covers.
3. **Order by impact**: Within each area, order entries by impact — breaking changes first, then new features, then bug fixes.
4. **Order areas by activity**: Place the area with the most entries first.

### Step 4: Audit Experimental API Changes

Follow [references/experimental-features.md](references/experimental-features.md):

1. For each candidate PR, **fetch the file list and diff** to identify changes to `[Experimental]` APIs. Do not infer experimental changes from PR titles — always verify against the actual files changed.
2. Classify each change: now stable, new experimental, breaking change to experimental, or removed.
3. Derive the conceptual feature name from the actual types/members affected in the diff.
4. Record in the `experimental_changes` SQL table.
5. Present findings to the user for confirmation.

### Step 5: Determine Package Versions

Build the package version information:

1. For **full releases**: all packages ship at the same version. Note the version number but do not generate a per-package table — it would be repetitive with no value.
2. For **patch releases**: build a table of only the affected packages and their version numbers.
3. Present the version information to the user for confirmation. The user may adjust which packages are included in a patch release.

### Step 6: Draft Release Notes

Compose the release notes following [references/format-template.md](references/format-template.md) and [references/editorial-rules.md](references/editorial-rules.md):

1. **Preamble** — Optionally draft 2–3 sentences summarizing the release theme. Present the preamble options to the user using the `ask_user` tool, offering them the choice of: (a) one of the suggested preambles, (b) writing their own, or (c) skipping the preamble entirely.
2. **Packages in this release** — for patch releases, the table of affected packages and versions from Step 5. For full releases, omit this table (all packages ship at the same version and listing them all adds no value).
3. **Breaking Changes** — stable API breaks only (should be very rare). Include migration guidance.
4. **Experimental API Changes** — from Step 4 results. Group by change type. Omit empty subsections.
5. **What's Changed** — area-grouped entries from Step 3. Omit empty areas.
6. **Documentation Updates** — chronological flat list.
7. **Test Improvements** — chronological flat list.
8. **Repository Infrastructure Updates** — chronological flat list.
9. **Acknowledgements** — new contributors, issue reporters, PR reviewers.
10. **Full Changelog** — link to the GitHub compare view.

Omit empty sections entirely.

### Step 7: Review and Finalize

Present the complete draft to the user:

1. The full release notes markdown
2. Summary statistics (number of PRs, packages affected, areas covered)
3. Any unresolved items (ambiguous PRs, missing package assignments)

After the user has reviewed and approved the draft, present the finalization options using the `ask_user` tool:
- **Create draft release** — create a GitHub release in draft state with the notes as the body
- **Save to private gist** — save the draft notes to a private GitHub gist for later use
- **Cancel** — discard the draft without creating anything

## Edge Cases

- **PR spans categories**: Categorize by primary intent; read the title and description.
- **PR spans multiple areas**: Place under the most central area; mention cross-cutting nature in the description.
- **Copilot-authored PRs**: If the PR author is Copilot or a bot, check the `copilot_work_started` timeline event for the triggering user, then assignees, then the merger. See [references/editorial-rules.md](references/editorial-rules.md) for the full fallback chain. Never fabricate an attribution — always derive it from the PR data.
- **No breaking changes**: Omit the Breaking Changes section entirely.
- **No experimental changes**: Omit the Experimental API Changes section entirely.
- **No user-facing changes**: If all PRs are documentation, tests, or infrastructure, note this in the release notes. The release still proceeds — this repository ships monthly regardless.
- **Patch release with unclear scope**: Ask the user to confirm which packages are included.
- **No previous release**: If this is the first release under the current versioning scheme, gather all PRs from the beginning of the tag history.
- **Version mismatch**: If the tag version doesn't match the version in source files, flag the discrepancy.
- **Large release (100+ PRs)**: Break the enrichment step into parallel batches. Consider summarizing lower-impact areas more aggressively.
- **Cross-repo changes**: Some PRs may reference issues or changes in other repos (e.g. `dotnet/runtime`). Use full markdown links for cross-repo references.
