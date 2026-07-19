# Stage 1 - Verify Source Link and Symbols

The publish-release playbook published the packages to nuget.org and promoted the shipping build to the public `.NET <major>` channel. Only after that promotion do the shipped symbols publish to the Microsoft symbol server (msdl). Verify Source Link and symbol-server availability -- this is the **release sign-off gate**.

## Prerequisites

- publish-release is complete: packages published to nuget.org and the shipping build promoted to the public `.NET <major>` channel.
- The `sourcelink` and `dotnet-symbol` global tools (`dotnet tool install -g sourcelink`; `dotnet tool install -g dotnet-symbol`).

## Verify

Run the Source Link sweep against the folder of published packages:

```
scripts/Test-SourceLink.ps1 -PackageDir <folder-with-published-.nupkg>
```

For each `.nupkg` the script extracts a lib DLL, pulls the matching PDB from the Microsoft symbol server (msdl) via `dotnet-symbol`, and runs `sourcelink test`. Each package reports one of:

- `valid` -- Source Link resolved and the symbols were on msdl.
- `sourcelink-FAILED` -- symbols found, but Source Link did not validate (investigate).
- `symbols-not-indexed` -- the PDB is not yet on msdl (still "Validating..." on nuget.info; re-run later).
- `no-lib-dll` -- template/tooling package with no `lib/**/*.dll` (expected).

Indexing on msdl lags the promotion, so `symbols-not-indexed` immediately afterward is expected -- **re-run until every library package is `valid`**. If packages stay `symbols-not-indexed` well after the promotion, re-confirm the shipping build is actually on the public `.NET <major>` channel (`darc get-build --id <bar-id>` should list `.NET <major>`); an internal-only build's symbols go to the internal isolated feed and never reach msdl. Investigate any `sourcelink-FAILED`.

**Do not sign off the release until every library package reports `valid`.**

## After the stage

Once every library package is `valid`, the release symbols are public. Continue with **Stage 2 - Reconcile Branches**.
