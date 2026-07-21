# Stage 2 - Update Dependencies

This stage is for the **monthly release**. Servicing releases should follow [servicing-release-prep.md](servicing-release-prep.md) instead.

Update the internal release branch's product dependencies to the pending .NET 9, .NET 8, and .NET 10 release updates using `darc update-dependencies` against Build Asset Registry (BAR) build IDs.

This stage has three sub-stages. Run them in order, and give each its own commit:

1. Update Dependencies: .NET 9
2. Update Dependencies: .NET 8
3. Update Dependencies: .NET 10

> **Why .NET 9 is first (this is intentional, not numeric order).** .NET 9 is the *coherent primary* band. `darc update-dependencies` and `eng/Version.Details.xml` write the **canonical, non-suffixed** version properties in `eng/Versions.props` (the `...Version` entries, which currently hold `9.0.x`), and only one band can own those canonical locations. So .NET 9 is applied first, on a clean tree, and its changes are kept wholesale -- `Version.Details.xml`, the non-suffixed `...Version` entries, and the base internal `NuGet.config` sources. .NET 8 (`...LTSVersion`, `8.0.x`) and .NET 10 (`...Net10Version`, `10.0.x`) are then layered on top as overlays: their darc writes to the canonical locations are reverted and the numbers hand-copied into the suffixed entries. That ordering is exactly why Sub-stages 2 and 3 tell you to *revert the 9.0 non-suffixed lines* and to keep the `NuGet.config` sources ".NET 9 added" -- those fix-up steps only make sense if .NET 9 has already run.

Commit each sub-stage automatically as you complete it -- do not pause for per-sub-stage review or approval. This overrides steps 2 and 3 of the [Stage workflow](../README.md#stage-workflow). Pushing remains a separate, user-directed step: do not push until the user explicitly instructs it (see "After the stage").

Before committing each sub-stage, review the diff yourself and revert any incidental, non-dependency changes darc introduces (for example, darc sometimes adds a trailing newline to `.config/dotnet-tools.json`). Keep only the intended dependency edits for that sub-stage.

## Prerequisites

- Confirm the working tree is clean and you are on the `stage-release-<major>.<minor>` branch.
- `darc` must be installed and authenticated against BAR:
  - If `darc` is not on the PATH, run `eng/common/darc-init.ps1` (Windows) or `eng/common/darc-init.sh`.
  - If darc commands fail with authentication errors, ask the user to run `darc authenticate` and supply a valid BAR token. Never store, echo, or commit tokens.

## Gathering the BAR build IDs (release.dot.net)

The Release Tracker at <https://release.dot.net/releases> is a Blazor WebAssembly app behind Microsoft (MSAL) authentication. The agent cannot reach or render it, so the **user** provides the inputs.

**Collect the inputs for all three sub-stages up front, in a single prompt, before running any `darc` commands.** For each pending release, the user opens it on the Release Tracker, clicks its `{}` artifacts link, and opens `manifests/ReleaseManifest.json`. Accept each one either pasted into the chat or as a local path to a downloaded copy:

- **.NET 9** -- the pending 9.0 release's `manifests/ReleaseManifest.json`.
- **.NET 8** -- the pending 8.0 release's `manifests/ReleaseManifest.json`.
- **.NET 10** -- the pending 10.0 release's `manifests/ReleaseManifest.json`, or a pasted list of its BAR build IDs from the release page.

Each build entry in a `ReleaseManifest.json` has a `repo` and a `barBuildId`. Read the IDs you need:

- **.NET 9 and .NET 8:** the `barBuildId` for `dotnet-runtime`, `dotnet-aspnetcore`, and `dotnet-efcore`.
- **.NET 10:** every `barBuildId` (or the pasted list). Order does not matter.

Validate every ID with `darc get-build --id <barId>` before applying it, and confirm the repository and commit look correct. If an ID does not resolve, stop and ask the user.

## Sub-stage 1 - Update Dependencies: .NET 9

For each of `dotnet-runtime`, `dotnet-aspnetcore`, and `dotnet-efcore`, using that release's `barBuildId`:

```
darc update-dependencies --id <barBuildId>
```

- `dotnet-efcore` commonly reports `warn: Found no dependencies to update` -- that is expected.
- .NET 9 is the coherent primary update: **keep everything darc changes**, including `eng/Version.Details.xml`, the non-suffixed `...Version` entries in `eng/Versions.props`, and the new internal package sources in `NuGet.config`.

Review the changes, then stage and commit:

```
git add .
git commit -m "Update 9.0 dependencies"
```

## Sub-stage 2 - Update Dependencies: .NET 8

For each of `dotnet-runtime`, `dotnet-aspnetcore`, and `dotnet-efcore`, using the 8.0 release's `barBuildId`:

```
darc update-dependencies --id <barBuildId>
```

`dotnet-efcore` commonly reports `warn: Found no dependencies to update`.

Once all three repos are applied, fix up the changes so that only the 8.0 (`...LTSVersion`) entries move:

1. Revert `Version.Details.xml` (the 8.0 update must not rewrite it):

   ```
   git checkout eng/Version.Details.xml
   ```

2. `NuGet.config` -- resolve keeping both. Keep the pre-existing internal package sources (added by .NET 9) **and** the newly added 8.0 ones; discard the deletions and keep the additions. The result adds the new 8.0 internal package sources: three in `<packageSources>` and three in `<disabledPackageSources>`. Then:

   ```
   git add NuGet.config
   ```

3. `eng/Versions.props` hand-edit:
   - Move the 8.0 versions darc produced into the matching `...LTSVersion` entries, replacing their previous values (that is, take the updated numbers from the non-suffixed `...Version` entries and write them into the corresponding `...LTSVersion` entries -- "Version>" becomes "LTSVersion>").
   - Revert the changes to the 9.0 (non-suffixed `...Version`) lines.
   - Revert changes to entries that have no 8.0 update: `Microsoft.Bcl.Memory`, `System.Numerics.Tensors`, `System.Memory.Data`.
   - Then `git add eng/Versions.props`.

4. Commit:

   ```
   git commit -m "Update 8.0 dependencies"
   ```

Only `NuGet.config` and `eng/Versions.props` belong in this commit.

## Sub-stage 3 - Update Dependencies: .NET 10

Apply each 10.0 BAR build ID (order does not matter):

```
darc update-dependencies --id <barId> --no-coherency-updates
```

Once all IDs are applied, fix up the changes:

1. Revert `Version.Details.xml`:

   ```
   git checkout eng/Version.Details.xml
   ```

2. Revert the Arcade / Helix / Build.Tasks.Templating tooling file changes under `eng/common/` (darc rewrites these because Arcade ships in the VMR):

   ```
   git checkout eng/common
   ```

3. `NuGet.config` -- resolve keeping both, exactly as in Sub-stage 2: keep the pre-existing and the newly added internal package sources. Then `git add NuGet.config`.

4. `eng/Versions.props` hand-edit:
   - **Keep** the Arcade `MicrosoftDotNetBuildTasksTemplating*Version` entry updates (both the base entry and the `...Net10Version` variant). These stay -- only the `eng/common/` tooling files and `Version.Details.xml` are reverted for Arcade.
   - Move the 10.0 versions darc produced into the matching `...Net10Version` entries, replacing their previous values ("Version>" becomes "Net10Version>").
   - Revert the changes to the 9.0 (non-suffixed `...Version`) lines.
   - Then `git add eng/Versions.props`.

5. Commit:

   ```
   git commit -m "Update 10.0 dependencies"
   ```

Only `NuGet.config` and `eng/Versions.props` belong in this commit -- no `eng/common/` files and no `Version.Details.xml`.

## After the stage

Do not push from within this stage. Pushing the `stage-release-<major>.<minor>` branch is a separate, user-directed step. When it happens, resolve the internal remote by its URL rather than assuming a remote name, which varies between clones.
