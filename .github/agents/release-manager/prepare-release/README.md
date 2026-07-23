# Prepare Release

Prepares a `dotnet/extensions` release for publication. This playbook supports **two preparation tracks**:

- **Monthly release** -- prepare a public branch (`release/<major>.<minor>`) for the internal release flow on `internal/release/<major>.<minor>`.
- **Servicing release** -- prepare directly on `release/<major>.<minor>` by selecting backports from `main` and bumping patch version.

## Choose the preparation track first

Before making changes, determine which track applies:

| Release type | Use this path |
|---|---|
| Monthly release | Stages 1-2 in this README |
| Servicing release (`<major>.<minor>.<patch>`) | [references/stages-1-2-servicing-branch.md](references/stages-1-2-servicing-branch.md) |

Do **not** mix tracks. Servicing releases should not run Stage 1/2 internal-branch prep unless the user explicitly asks to override the servicing workflow.

## Stage workflow (monthly release)

Complete stages strictly in order. Treat each stage -- and each sub-stage of a stage that has them -- as an independent, committable unit. For every stage or sub-stage:

1. Apply the changes described in the stage's reference file.
2. Prompt the user to review the changes (summarize what changed and show the diff), unless the stage's reference file directs you to commit automatically.
3. Wait for the user's approval before committing, unless the stage's reference file directs automatic commits.
4. Create a single commit that contains only that stage's (or sub-stage's) changes.

Every stage gets its own commit, and every sub-stage gets its own commit. Never combine multiple stages or sub-stages into a single commit. Never push until the user explicitly instructs it.

Commits this playbook creates land in public dotnet/extensions history -- they flow out later through the non-squash internal-to-public merge (validate-release, Stage 6). Keep the `Co-authored-by: Copilot` trailer on those commits but omit the `Copilot-Session` trailer.

## Stage 1 - Prepare Internal Branch (monthly release)

Apply the internal-release infrastructure changes to the branch: suppress `NU1507`, remove the NuGet package source mapping, switch on stable/release versioning, add private-feed credential setup to the build template, comment out integration tests, and remove the code-coverage pipeline stage. Never change version numbers here -- those flow via Dependency Flow automation.

Read and follow [references/stage-1-prepare-internal-branch.md](references/stage-1-prepare-internal-branch.md).

## Stage 2 - Update Dependencies (monthly release)

Update the branch's product dependencies to the pending .NET 9, .NET 8, and .NET 10 servicing releases using `darc update-dependencies`. The BAR build IDs come from the release.dot.net Release Tracker, which is behind Microsoft auth and unreachable by the agent, so the user supplies them (pasted `ReleaseManifest.json` or a downloaded copy). This stage has three sub-stages, each its own commit: .NET 9, then .NET 8, then .NET 10.

Read and follow [references/stage-2-update-dependencies.md](references/stage-2-update-dependencies.md).

## Servicing preparation workflow (servicing release)

For servicing releases, do not use Stage 1/2 above. Instead, follow:

- [references/stages-1-2-servicing-branch.md](references/stages-1-2-servicing-branch.md)

That flow covers commit selection from `main`, patch-version bumping, servicing PR composition, and package-scope confirmation for later publish/validate/release-notes stages.

## Next

- Monthly release: once Stages 1-2 are committed and reviewed, continue with **publish-release** (land, publish, promote) and **validate-release** (verify symbols, reconcile branches).
- Servicing release: once the servicing prep PR merges into `release/<major>.<minor>`, continue with **publish-release** (mirror + official build + publish selected scope), then **validate-release** and **write-release-notes**.
