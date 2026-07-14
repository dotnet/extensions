# Stage 4 - Publish and Verify

After Stage 3 lands the release on `internal/release/<major>.<minor>` and its official build produces the `PackageArtifacts`, publish the packages to nuget.org and verify Source Link on the shipped symbols.

Like Stage 3, this stage is operational and produces no commit. Publishing is **human-gated and irreversible**, so the agent prepares and reviews the package set but never runs `dotnet nuget push` itself and never handles API keys. The Source Link verification is fully automated.

## Prerequisites

- Stage 3 complete: the official build on `internal/release/<major>.<minor>` succeeded and produced the `PackageArtifacts`.
- For verification: the `sourcelink` and `dotnet-symbol` global tools (`dotnet tool install -g sourcelink`; `dotnet tool install -g dotnet-symbol`).

## Sub-stage 1 - Prepare and publish packages

Publishing is irreversible (a published version cannot be overwritten or truly deleted) and requires nuget.org API keys, so the agent only prepares the set; the user runs the push.

1. Download and extract the `PackageArtifacts` from the official build.
2. Stage the packages to publish into a clean folder, **excluding**:
   - `Microsoft.Internal.*` (always).
   - Any packages the user names to hold back for this release. Confirm the exact list, and flag template/tooling packages (for example `*.ProjectTemplates`) for an explicit decision.
3. Present the excluded and to-publish lists for the user to review.
4. Two nuget.org accounts are involved: almost all packages publish from the **dotnetframework** account; **`Microsoft.Agents.AI.ProjectTemplates`** publishes from the **MicrosoftAgentFramework** account, so it must be pushed separately with that account's key.
5. The **user** runs `dotnet nuget push` with the appropriate API key(s). Never run the push, and never handle the API keys.

## Sub-stage 2 - Verify Source Link and symbols

Once the packages and their symbols are published (symbol publishing happens via the channel-promotion step and lags the nuget push), verify Source Link and symbol-server availability:

```
scripts/Test-SourceLink.ps1 -PackageDir <folder-with-published-.nupkg>
```

Each package reports `valid`, `sourcelink-FAILED`, `symbols-not-indexed`, or `no-lib-dll` (template/tooling package). Symbol indexing on nuget.org lags publishing, so `symbols-not-indexed` right after publish is expected -- **re-run until every library package is `valid`**. Investigate any `sourcelink-FAILED`.

## After the stage

Stage 4 produces no repository commit. The internal-to-public and release-to-main merges are handled by Stage 5. The remaining activities (dotnet-public mirror, release notes and tag, and the support-page update) are outside the scope of these stages.
