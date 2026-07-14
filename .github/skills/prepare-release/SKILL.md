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
2. Prompt the user to review the changes (summarize what changed and show the diff), unless the stage's reference file directs you to commit automatically.
3. Wait for the user's approval before committing, unless the stage's reference file directs automatic commits.
4. Create a single commit that contains only that stage's (or sub-stage's) changes.

Every stage gets its own commit, and every sub-stage gets its own commit. Never combine multiple stages or sub-stages into a single commit. Never push until the user explicitly instructs it, whatever a stage's commit cadence. Stage 3 is the exception: it is operational -- it pushes existing commits and queues pipelines rather than producing a commit of its own.

## Stage 1 - Prepare Internal Branch

Apply the internal-release infrastructure changes to the branch: suppress `NU1507`, remove the NuGet package source mapping, switch on stable/release versioning, add private-feed credential setup to the build template, comment out integration tests, and remove the code-coverage pipeline stage. Never change version numbers here -- those flow via Dependency Flow automation.

Read and follow [references/stage-1-prepare-internal-branch.md](references/stage-1-prepare-internal-branch.md).

## Stage 2 - Update Dependencies

Update the branch's product dependencies to the pending .NET 9, .NET 8, and .NET 10 servicing releases using `darc update-dependencies`. The BAR build IDs come from the release.dot.net Release Tracker, which is behind Microsoft auth and unreachable by the agent, so the user supplies them (pasted `ReleaseManifest.json` or a downloaded copy). This stage has three sub-stages, each its own commit: .NET 9, then .NET 8, then .NET 10.

Read and follow [references/stage-2-update-dependencies.md](references/stage-2-update-dependencies.md).

## Stage 3 - Validate and Land

Validate the prepared `stage-release-<major>.<minor>` branch with an official build, then land it on the `internal/release/<major>.<minor>` branch. This stage is operational -- it pushes existing commits and queues the `official-build-pipeline` pipeline rather than creating new commits -- so it runs only on explicit user instruction and pauses before every push and land action. It gates landing on a green stage-branch pipeline and never auto-completes a pull request that needs elevation.

Read and follow [references/stage-3-validate-and-land.md](references/stage-3-validate-and-land.md).
