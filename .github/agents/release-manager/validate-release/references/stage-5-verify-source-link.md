# Stage 5 - Verify Package Dependencies, Source Link, and Symbols

The publish-release playbook published the packages to nuget.org and promoted/assigned the official
release build to the public `.NET <major>` channel. Verify exact-version dependency resolution first.
Only after channel assignment do symbols publish to the Microsoft symbol server (msdl), so then verify
Source Link and symbol-server availability. Both checks are **release sign-off gates**.

## Prerequisites

- publish-release is complete: packages published to nuget.org and the official release build assigned/promoted to the public `.NET <major>` channel.
- The `sourcelink` and `dotnet-symbol` global tools (`dotnet tool install -g sourcelink`; `dotnet tool install -g dotnet-symbol`).
- The final published manifest from publish-release is known. It is authoritative for both tracks and
  identifies selected packages separately from dependency-closure packages. For servicing releases,
  do not fall back to the provisional package list in the servicing-prep PR.

## Sub-stage 1 - Verify exact-version dependency resolution

Follow [package scope and dependency validation](../../references/package-scope-and-dependency-validation.md).

Apply the shared invariant to every package in the final published manifest, record its required
resolution report, and obtain explicit approval before Source Link verification. Allow bounded
retries for expected propagation delay, but do not proceed with unresolved dependencies.

## Sub-stage 2 - Verify Source Link and symbols

Run the Source Link sweep against the folder containing the published packages for this release scope:

```
./.github/agents/release-manager/validate-release/scripts/Test-SourceLink.ps1 -PackageDir <folder-with-published-.nupkg>
```

For each `.nupkg` the script extracts a lib DLL, pulls the matching PDB from the Microsoft symbol server (msdl) via `dotnet-symbol`, and runs `sourcelink test`. Each package reports one of:

- `valid` -- Source Link resolved and the symbols were on msdl.
- `sourcelink-FAILED` -- symbols found, but Source Link did not validate (investigate).
- `symbols-not-indexed` -- the PDB is not yet on msdl (still "Validating..." on nuget.info; re-run later).
- `no-lib-dll` -- template/tooling package with no `lib/**/*.dll` (expected).

Indexing on msdl lags the promotion/channel assignment, so `symbols-not-indexed` immediately afterward is expected -- **re-run until every published library package in scope is `valid`**. If packages stay `symbols-not-indexed` well after publish, re-confirm the official release build is actually on the public `.NET <major>` channel (`darc get-build --id <bar-id>` should list `.NET <major>`). Investigate any `sourcelink-FAILED`.

**Do not sign off the release until every published package's exact-version dependencies resolve and
every published library package in scope reports `valid`.**

## After the stage

Once dependency resolution passes and every library package is `valid`, the package graph and symbols
are public:

- **Monthly release:** continue with **Stage 6 - Reconcile Branches**.
- **Servicing release:** continue with **Stage 7 - Support-Page Follow-up** unless the user explicitly
  requested Stage 6 reconciliation.
