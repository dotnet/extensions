# Stage 6 - Reconcile Branches

After Stage 5 verifies the published symbols, reconcile the branches: merge the internal release branch back out to the public release branch, then merge the public release branch into `main`. This is the final release activity.

This stage is designed for the **monthly release**. For servicing releases prepared directly on public `release/<major>.<minor>`, Stage 6 is usually skipped unless the user explicitly asks for additional branch-flow reconciliation.

Like the publish-release stages, this stage is operational: it stages merges and creates commits on throwaway `merge/*` branches, but it does **not** push or complete pull requests on its own. The agent stages each merge, applies the required file "doctoring", commits, and shows the diff for review; **pushing and completing the PRs (which need JIT elevation / admin settings) are user-directed**, and each merge PR must land as a merge commit (never squashed).

Run each sub-stage only when the user instructs it. Sub-stage 2 depends on Sub-stage 1's PR having already merged into the public release branch.

## Prerequisites

- Stage 4 complete: packages published and the release is public.
- A clone with the public GitHub remote (referred to here as `dotnet`) and up-to-date `internal/release/<major>.<minor>`, `release/<major>.<minor>`, and `main` branches. Fetch all three first.

## Sub-stage 1 - Merge internal release branch into the public release branch

Carry the version bumps and internal product changes out to the public `release/<major>.<minor>` branch while discarding the internal-only infrastructure changes (the Stage 1 prep).

1. From `release/<major>.<minor>`, create the merge branch (do not use a `release/` prefix -- it is protected):

   ```
   git switch release/<major>.<minor>
   git switch -c merge/<major>.<minor>
   ```

2. Stage the merge without committing:

   ```
   git merge --no-ff --no-commit internal/release/<major>.<minor>
   ```

3. Discard the internal-only infrastructure changes so they do not reach the public branch -- restore these to the public release-branch version:

   ```
   git checkout HEAD -- Directory.Build.props NuGet.config azure-pipelines.yml eng/pipelines/templates/BuildAndTest.yml
   ```

4. Keep everything else staged, in particular the versioning changes (`eng/Version.Details.xml`, `eng/Versions.props`) and any internal-only product changes / backports.

5. Compare against the last five releases to check for anomalies vs. consistency. Look at the corresponding merge PRs from the previous five monthly releases (their titles vary -- e.g. "Merge published release into release/X.Y", "Merging internal changes into the release/X.Y branch", "Merge internal changes from X.Y"). Find them and list their changed files with `gh`:

   ```
   gh pr list --repo dotnet/extensions --base release/<earlier>.<minor> --state merged --json number,title
   gh pr view <number> --repo dotnet/extensions --json files
   ```

   Compare this merge's staged file set (`git diff --cached --stat`) against them and flag anomalies to the user before committing:
   - `eng/Version.Details.xml` and `eng/Versions.props` appear in every release and must be here too (including the stabilization flip).
   - The internal-only infrastructure files (`Directory.Build.props`, `NuGet.config`, `azure-pipelines.yml`, `eng/pipelines/templates/BuildAndTest.yml`) are absent in every recent release and must be absent here too.
   - Tooling files such as `.github/agents/**` and `.github/skills/**` are excluded by recent releases -- flag if present.
   - The remaining files should be version bumps plus intentional product/test backports. Flag anything unexpected: an unusually large or empty change set, missing versioning, leaked infrastructure, or files no prior release touched.

6. Review the staged diff: it should be version bumps + product changes, with **no** infrastructure changes. Then commit:

   ```
   git commit -m "Merge published release into release/<major>.<minor>"
   ```

7. Pushing (user-directed) to `dotnet` and opening the PR into `release/<major>.<minor>` -- title "Merge published release into release/<major>.<minor>", label `DO-NOT-SQUASH`, description "Merge using a merge commit. Do not squash." Do not enable auto-merge (squash). Completing the PR (JIT elevation, allow merge commits) is done by the user.

## Sub-stage 2 - Merge the public release branch into main

Do this only after Sub-stage 1's PR has merged into `release/<major>.<minor>`. It reverts the stable-versioning flip so `main` does not produce stable/release versions.

1. Fetch, then from `main` create the merge branch:

   ```
   git switch main
   git switch -c merge/<major>.<minor>-to-main
   ```

2. Stage the merge without committing:

   ```
   git merge --no-ff --no-commit release/<major>.<minor>
   ```

3. In `eng/Versions.props`, revert only the package-stabilization change (keep all version numbers and all `eng/Version.Details.xml` changes):
   - Set `StabilizePackageVersion` back to `false`.
   - Set `DotNetFinalVersionKind` back to empty (`<DotNetFinalVersionKind />`).
   - `git add eng/Versions.props`.

4. Review the staged diff, then commit:

   ```
   git commit -m "Merge release/<major>.<minor> into main"
   ```

5. Pushing (user-directed) to `dotnet` and opening the PR into `main` -- title "Merge release/<major>.<minor> into main", label `DO-NOT-SQUASH`, description "Merge using a merge commit. Do not squash." Do not enable auto-merge (squash). The user completes the PR.

## After the stage

This stage produces merge commits on `merge/*` branches but does not push or complete the pull requests. Tagging and publishing the release notes are handled by the **write-release-notes** playbook. Next, confirm the support-page listing (Stage 7).
