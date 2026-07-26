# Stage 3 - Build (Servicing Release)

Use this Stage 3 variant for servicing releases prepared directly on `release/<major>.<minor>`.

Unlike the monthly release flow, there is no `stage-release-*` -> `internal/release/*` landing step. The servicing-prep PR merges into the public release branch first, then that branch is mirrored to AzDO.

## Prerequisites

- The servicing-prep PR into `release/<major>.<minor>` is merged.
- You know the package scope from that PR description.
- You have access to Azure DevOps and the `extensions-ci-official` pipeline.

## Steps

1. Confirm the merged commit on `release/<major>.<minor>`.
2. Wait for that commit to mirror into AzDO's `dotnet-extensions` repository.
3. Trigger `extensions-ci-official` from `refs/heads/release/<major>.<minor>` (or the mirrored equivalent branch ref).
4. Wait for a successful official run producing release `PackageArtifacts`.
5. Record and share with the user:
   - AzDO build run URL
   - AzDO build ID / build number
   - BAR build ID for the official release build

## Notes

- This stage is operational and irreversible once publishing starts downstream.
- Do not proceed to Stage 4 publishing until the official run is green and artifact identity (commit + BAR id) is confirmed.
- Do not push/merge anything from this stage unless the user explicitly asks.

## After the stage

Proceed to **Stage 4 - Publish and Promote** using:

- the official release build from this stage, and
- the servicing package scope captured in the merged servicing-prep PR description.
