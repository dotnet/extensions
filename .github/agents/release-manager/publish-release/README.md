# Publish Release

Publishes a prepared `dotnet/extensions` release. This playbook supports both tracks:

- **Monthly release**: land the prepared branch on `internal/release/<major>.<minor>`, then publish and promote.
- **Servicing release**: after servicing-prep PR merge, wait for mirror, run official build from `release/<major>.<minor>`, then publish selected packages.

Run this playbook after **prepare-release** is complete. Both stages are **operational** -- they push, queue pipelines, publish, and promote rather than producing ordinary commits -- and every push, publish, and promotion is **irreversible**. Run each stage only on explicit user instruction, and pause for confirmation before every irreversible action.

## Stage 3 - Build

Stage 3 has two variants:

- **Monthly release**: land the prepared `stage-release-<major>.<minor>` branch on `internal/release/<major>.<minor>`, then run the official build that produces `PackageArtifacts`.
  Follow [references/stage-3-build-monthly.md](references/stage-3-build-monthly.md).
- **Servicing release**: after the servicing-prep PR merges into `release/<major>.<minor>`, wait for mirror and run `extensions-ci-official` from that public release branch.
  Follow [references/stage-3-build-servicing.md](references/stage-3-build-servicing.md).

## Stage 4 - Publish and Promote

Publish the packages to nuget.org (the user runs `dotnet nuget push`; the agent never handles API keys), then ensure the official release build is on the public `.NET <major>` channel so symbols reach msdl.

For servicing releases, use the merged servicing-prep PR description as the source of truth for package scope (what to publish and what to exclude) unless the user explicitly changes it.

Read and follow [references/stage-4-publish-and-promote.md](references/stage-4-publish-and-promote.md).

## Next

Once the packages are published and channel assignment is confirmed, run **validate-release** to verify symbols on msdl and complete post-release checks.
