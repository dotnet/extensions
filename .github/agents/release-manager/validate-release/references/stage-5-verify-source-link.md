# Stage 5 - Verify Source Link and Symbols

The publish-release playbook published the packages to nuget.org and promoted/assigned the official release build to the public `.NET <major>` channel. Only after that assignment do symbols publish to the Microsoft symbol server (msdl). Verify Source Link and symbol-server availability -- this is the **release sign-off gate**.

## Prerequisites

- publish-release is complete: packages published to nuget.org and the official release build assigned/promoted to the public `.NET <major>` channel.
- The `sourcelink` and `dotnet-symbol` global tools (`dotnet tool install -g sourcelink`; `dotnet tool install -g dotnet-symbol`).
- The package scope for this release is known:
  - Monthly release: the full release package set.
  - Servicing release: the package list from the merged servicing-prep PR description (unless the user explicitly changed scope at publish time).

## Verify

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

**Do not sign off the release until every published library package in scope reports `valid`.**

## After the stage

Once every library package is `valid`, the release symbols are public. Continue with **Stage 6 - Reconcile Branches**.
