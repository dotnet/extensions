# Stage 1 - Land

After the prepare-release playbook is committed and reviewed (Prepare Internal Branch, then Update Dependencies), land the `stage-release-<major>.<minor>` branch on `internal/release/<major>.<minor>` -- gated on a green official build.

This stage is operational, not file-editing: it pushes existing commits and queues pipelines rather than producing new commits. Because it pushes to the internal remote and can land changes irreversibly, it runs **only on explicit user instruction** and pauses for confirmation before every push, pipeline queue, and land action. It produces no commit of its own.

## Prerequisites

- The prepare-release playbook is complete and the working tree is clean on `stage-release-<major>.<minor>`.
- Push access to the internal release repository, and access to run its official build pipeline.

## Resolve the internal remote

The internal remote is distinct from the public GitHub remote (`dotnet`). Resolve it by its URL rather than by name -- the remote name varies between clones (`git remote -v`). It is referred to below as `<internal-remote>`.

## Sub-stage 1 - Run CI on the stage branch

Do this only when the user instructs you to push.

1. Push the stage branch to the internal remote:

   ```
   git push <internal-remote> stage-release-<major>.<minor>
   ```

2. Run the official build pipeline on the stage branch and wait for it to complete **successfully**. Do not land the branch until that run is confirmed green. If it fails, investigate, fix on the stage branch (each fix is a fresh commit), and re-run.

## Sub-stage 2 - Land on the internal release branch

Land only after the stage-branch pipeline is confirmed successful, and only when the user has chosen an option and confirmed. There are two options.

### Option 1 - Direct push (bypasses PR review)

Only valid once the stage-branch pipeline is green -- that run is the only readiness gate:

```
git push <internal-remote> stage-release-<major>.<minor>:internal/release/<major>.<minor>
```

### Option 2 - Internal pull request

- Create a pull request from `stage-release-<major>.<minor>` targeting `internal/release/<major>.<minor>` on the internal remote.
- Complete it with **Rebase and fast-forward** (not Squash), preserving each sub-stage commit as a linear history so it matches Option 1's direct push and keeps each sub-stage's actions auditable. Do not squash.

Never auto-complete a pull request that requires JIT elevation or repository admin-setting changes; hand those actions to the user.

## Sub-stage 3 - Run the official build

After the changes land on `internal/release/<major>.<minor>`, run the official build pipeline on that branch to produce the official release build whose artifacts will be published.

## After the stage

This stage produces no repository commit. Once the official build on `internal/release/<major>.<minor>` succeeds and produces the `PackageArtifacts`, continue with **Stage 2 - Publish and Promote**.
