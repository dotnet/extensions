# Stage 4 - Publish and Promote

After the Land stage (publish-release Stage 3) lands the release on `internal/release/<major>.<minor>` and its official build produces the `PackageArtifacts`, publish the packages to nuget.org and promote the shipping build to the public channel so its symbols publish to the Microsoft symbol server (msdl).

This stage is operational and produces no commit. Both actions are **irreversible**: publishing is human-gated (the agent prepares and reviews the package set but never runs `dotnet nuget push` itself and never handles API keys), and the channel promotion runs only after the user confirms. Verifying that the shipped symbols actually landed on msdl is the next playbook -- **validate-release**.

## Prerequisites

- The Land stage is complete: the official build on `internal/release/<major>.<minor>` succeeded and produced the `PackageArtifacts`.
- For the channel promotion: the `darc` CLI, authenticated to the Build Asset Registry (BAR).

## Sub-stage 1 - Prepare and publish packages

Publishing is irreversible (a published version cannot be overwritten or truly deleted) and requires nuget.org API keys, so the agent only prepares the set; the user runs the push.

1. Download and extract the `PackageArtifacts` from the official build.
2. Stage the packages to publish into a clean folder, **excluding**:
   - `Microsoft.Internal.*` (always).
   - Any packages the user names to hold back for this release. Confirm the exact list, and flag template/tooling packages (for example `*.ProjectTemplates`) for an explicit decision.
3. Present the excluded and to-publish lists for the user to review.
4. Two nuget.org accounts are involved: almost all packages publish from the **dotnetframework** account; **`Microsoft.Agents.AI.ProjectTemplates`** publishes from the **MicrosoftAgentFramework** account, so it must be pushed separately with that account's key.
5. The **user** runs `dotnet nuget push` with the appropriate API key(s). Never run the push, and never handle the API keys.

## Sub-stage 2 - Promote the shipping build to the public channel

The packages you just published to nuget.org were built by the official build on `internal/release/<major>.<minor>`. A darc **default-channel** rule auto-assigns that build to the **`.NET <major> Internal`** channel, whose symbols publish to an internal isolated feed -- **not** the public Microsoft symbol server (msdl). The public **`.NET <major>`** channel (which does publish symbols to msdl) is only auto-assigned to builds from the *public* `release/<major>.<minor>` branch, and **no subscription promotes the internal build to it**. So the shipping build's symbols reach msdl only after you **manually promote it** to the public channel. Skipping this leaves the shipped packages with no public symbols / Source Link -- the failure this sub-stage exists to prevent.

1. Identify the shipping build's **BAR id** -- the BAR build for the official `internal/release/<major>.<minor>` build whose artifacts you published. Confirm its commit matches the `repository/commit` embedded in a shipped `.nuspec`. `darc get-build --id <bar-id>` should show it on `.NET <major> Internal` and typically **not** yet on `.NET <major>`.
2. Find the public channel id: `darc get-channels` -> the entry named exactly `.NET <major>` (no `Internal`/`Eng`/`Private`/`Workload` suffix).
3. Promote the build to the public channel with `darc add-build-to-channel --id <bar-id> --channel ".NET <major>"` -- **confirm the BAR id and channel with the user before triggering**, since this publishes its symbols to msdl (`SymbolTargetType: Public`) **and** flows its packages downstream to that channel, and is **irreversible once packages flow**. A non-blocking "Build validation audit failure for production channel" warning is expected.
4. Wait for the promotion build to complete, then confirm: `darc get-build --id <bar-id>` now lists `.NET <major>` among its channels.

## After the stage

This stage produces no repository commit. Next, run the **validate-release** playbook: verify Source Link and symbols on msdl (Stage 5), reconcile the branches (Stage 6), and confirm the support-page listing (Stage 7).
