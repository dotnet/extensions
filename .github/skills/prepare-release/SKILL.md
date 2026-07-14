---
name: prepare-release
description: Prepares the repository for an internal release branch. Use this when asked to "prepare for a release", "prepare internal release branch", or similar release preparation tasks.
---

# Prepare Release

Prepares a `dotnet/extensions` public release branch (`release/<major>.<minor>`) for the internal release process on the corresponding `internal/release/<major>.<minor>` branch.

The preparation is organized into ordered stages. Work through them in sequence, loading each stage's reference file only when you reach that stage.

## Stage workflow

Complete stages strictly in order. Treat each stage -- and each sub-stage of a stage that has them -- as an independent, committable unit. For every stage or sub-stage:

1. Apply the changes described in the stage's reference file.
2. Prompt the user to review the changes for that stage (summarize what changed and show the diff).
3. Wait for the user's approval before committing.
4. Create a single commit that contains only that stage's (or sub-stage's) changes.

Every stage gets its own commit, and every sub-stage gets its own commit. Never combine multiple stages or sub-stages into a single commit.

## Stage 1 - Prepare Internal Branch

Apply the internal-release infrastructure changes to the branch: suppress `NU1507`, remove the NuGet package source mapping, switch on stable/release versioning, add private-feed credential setup to the build template, comment out integration tests, and remove the code-coverage pipeline stage. Never change version numbers here -- those flow via Dependency Flow automation.

Read and follow [references/stage-1-prepare-internal-branch.md](references/stage-1-prepare-internal-branch.md).
