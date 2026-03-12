# Experimental Feature Tracking

The `dotnet/extensions` repository makes heavy use of the `[Experimental]` attribute to mark APIs that are not yet stable. Experimental APIs have their own diagnostic IDs and may undergo breaking changes, graduation to stable, or removal between releases. These changes are noteworthy and deserve dedicated coverage in release notes.

## Diagnostic ID conventions

Experimental APIs in this repository use diagnostic IDs documented in [`docs/list-of-diagnostics.md`](../../../docs/list-of-diagnostics.md) under the "Experiments" section. Consult that file for the current list of experimental diagnostic IDs and their descriptions. New diagnostic IDs may be added as new experimental features are introduced.

## Types of experimental changes

### Now Stable

An experimental API has its `[Experimental]` attribute removed, making it a stable part of the public API. This is a positive signal — the API has been validated through preview usage and feedback.

**How to detect:**
- PR removes `[Experimental("...")]` attribute from types or members
- PR removes the diagnostic ID from the project's `DiagnosticId` list
- PR description or title mentions "promote", "graduate", "stabilize", or "remove experimental"
- The corresponding `.json` API baseline file changes a type's `"Stage"` from `"Experimental"` to `"Stable"`

**How to report:**
Reference the feature by a conceptual name, not individual type names. Do not attribute to an author.
```markdown
* <Feature Name> APIs are now stable (previously `EXTEXP0003`) #PR
```

### Removed

An experimental API is removed entirely. This is acceptable under the experimental API contract — consumers who suppressed the diagnostic accepted this possibility.

**How to detect:**
- PR deletes types or members that were annotated with `[Experimental]`
- PR description mentions "remove" along with experimental type names
- The `.json` API baseline file removes entries that were previously `"Stage": "Experimental"`

**How to report:**
Reference the feature by a conceptual name, not individual type names. Do not attribute to an author.
```markdown
* <Feature Name> experimental APIs removed (was experimental under `MEAI001`) #PR
```

### Breaking changes to experimental APIs

An experimental API changes its signature, behavior, or contracts. These changes are acceptable under the experimental API policy but consumers need to know.

**How to detect:**
- PR modifies the signature of types/members annotated with `[Experimental]`
- PR changes behavior described in XML docs for experimental types
- PR renames experimental types or changes their namespace

**How to report:**
```markdown
* <Feature Name>: `TypeOrMemberName` signature changed (experimental under `EXTEXP0002`) #PR by @author
```

### New experimental APIs

A new API is introduced with the `[Experimental]` attribute. These are interesting for early adopters.

**How to detect:**
- PR adds new types or members annotated with `[Experimental]`
- PR introduces a new diagnostic ID
- The `.json` API baseline file adds entries with `"Stage": "Experimental"`

**How to report:**
```markdown
* New experimental API: <Feature Name> (`MEAI002`) #PR by @author
```

## Detection strategy

For each candidate PR, detect experimental API changes using the **PR diff** and the **`run-apichief` skill**. Do not rely on PR descriptions or PR labels for detection — they rarely mention experimental changes explicitly.

1. **Check the PR diff** using `pull_request_read` with method `get_files`. Look for changes to files containing `[Experimental` annotations. Specifically:
   - Files adding or removing `[Experimental("...")]` attributes
   - Changes to `.json` API baseline files where the `"Stage"` field changes between `"Experimental"` and `"Stable"`
   - Deletions of types or members that were previously experimental
2. **Cross-reference with `run-apichief`** — use the `run-apichief` skill's `emit delta` or `check breaking` commands to compare API baselines between the previous release and the current target. This reveals:
   - New experimental types/members added
   - Experimental types/members removed
   - Experimental types/members that changed stage to Stable
   - Signature changes on experimental types/members
3. **Cross-reference `docs/list-of-diagnostics.md`** — check if the PR modifies the diagnostics list, which signals addition or removal of experimental diagnostic IDs.

Store detected changes in the `experimental_changes` SQL table (see [sql-storage.md](sql-storage.md)).

## Presentation in release notes

Experimental feature changes appear in a dedicated section near the top of the release notes, after any stable breaking changes (which should be rare) and before the area-grouped "What's Changed" sections.

Group experimental changes by type:

```markdown
## Experimental API Changes

### Now Stable
* HTTP Logging Middleware APIs are now stable (previously `EXTEXP0013`) #7380

### New Experimental APIs
* Realtime Client Sessions (`MEAI001`) #7285 by @author

### Breaking Changes to Experimental APIs
* AI Function Approvals: `FunctionCallApprovalContext` constructor changed (experimental under `MEAI001`) #7350 by @author

### Removed Experimental APIs
* AI Tool Reduction experimental APIs removed (was experimental under `MEAI001`) #7300
```

Omit subsections that have no entries.
