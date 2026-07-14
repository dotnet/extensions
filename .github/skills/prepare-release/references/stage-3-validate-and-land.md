# Stage 3 - Validate and Land

After Stages 1 and 2 are committed and reviewed, validate the `stage-release-<major>.<minor>` branch with an official build and land it on the `internal/release/<major>.<minor>` branch.

This stage is operational, not file-editing: it pushes existing commits and queues pipelines rather than producing new commits. Because it pushes to the internal remote and can land changes irreversibly, it runs **only on explicit user instruction** and pauses for confirmation before every push, pipeline queue, and land action. This overrides the commit-per-stage cadence in `SKILL.md` -- Stage 3 produces no commit of its own.

## Prerequisites

- Stages 1 and 2 are complete and the working tree is clean on `stage-release-<major>.<minor>`.
   # run the official build pipeline on the branch and confirm it succeeds
- You have push access to the internal release repository.

## Resolve the internal remote

The internal remote is the one whose URL is `<internal-remote-url>`. Resolve it by URL rather than by name -- the remote name varies between clones:

```
git remote -v
```

Use the remote name that maps to that URL (referred to below as `<internal-remote>`).

## Sub-stage 1 - Validate the stage branch

Do this only when the user instructs you to push.

1. Push the stage branch to the internal remote:

   ```
   git push <internal-remote> stage-release-<major>.<minor>
   ```

2. Queue the official build pipeline against the stage branch:

   ```
   # run the official build pipeline on the branch and confirm it succeeds
   ```

3. Poll the run until it completes and confirm the result is `succeeded`:

   ```
   # run the official build pipeline on the branch and confirm it succeeds
   ```

   Do not land the branch until this run is confirmed green. If it fails, investigate, fix on the stage branch (each fix is a fresh commit), and re-run.

## Sub-stage 2 - Land on the internal release branch

Land only after the stage-branch pipeline is confirmed successful, and only when the user has chosen an option and confirmed. There are two options.

### Option 1 - Direct push (bypasses PR review)

Only valid once the stage-branch pipeline is green -- that run is the only readiness gate:

```
git push <internal-remote> stage-release-<major>.<minor>:internal/release/<major>.<minor>
```

### Option 2 - Internal pull request

- Create a pull request from `stage-release-<major>.<minor>` targeting `internal/release/<major>.<minor>` at `<internal-remote-url>`.
- Complete it with a SQUASH commit titled `Prepare <major>.<minor> release`, with the detailed commit message cleared.

Never auto-complete a pull request that requires JIT elevation or repository admin-setting changes; hand those actions to the user.

## Sub-stage 3 - Run the official build

After the changes land on `internal/release/<major>.<minor>`, queue `official-build-pipeline` against that branch to produce the official release build whose artifacts will be published:

```
   # run the official build pipeline on the branch and confirm it succeeds
```

## After the stage

Stage 3 produces no repository commit. Once the official build on `internal/release/<major>.<minor>` succeeds, the branch is ready for the release-publication steps (publishing packages, channel promotion, release notes), which are outside this skill.
