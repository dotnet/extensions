# Package Scope and Dependency Validation

Whenever package scope is determined:

1. Separate independently changed or human-selected packages (**Selected**) from packages included
   only because an in-scope package depends on them (**Dependency closure**).
2. Recursively include every direct/transitive dependency produced by `dotnet/extensions` that is
   required at the release version. Record the requiring parent and exact dependency requirement.
3. Present both groups separately and obtain explicit human approval. Recompute after every scope
   adjustment; a dependency-required package cannot be held back.

Monthly releases start with all releasable packages. Servicing releases start with selected roots.
Preparation may use project metadata provisionally, but the official `PackageArtifacts` `.nuspec`
files determine the final closure and published manifest.

Before publishing, verify every in-scope exact-version dependency resolves from staged artifacts or
approved sources. After propagation, repeat the check for every published package from clean,
consumer-visible sources, including packages without `lib` DLLs. Record the source and result.
Unresolved dependencies block publishing, validation, and release-note finalization.
