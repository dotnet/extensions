# Change Classification

Taxonomy for classifying gen-ai changes from semantic-conventions releases. Use this to assess each change's impact on dotnet/extensions.

## Classification Categories

### 🟢 No Action Required

| Type | Description | Example |
|------|-------------|---------|
| **N/A — No client exists** | Change affects a capability we don't implement (e.g. `retrieval`, `memory`) | `gen_ai.retrieval.*` attributes |
| **Already implemented** | Change was already implemented in a prior PR | A change that was part of an earlier draft spec we adopted |
| **Server-side only** | Change affects server/provider-side instrumentation, not client-side | Server span attributes |
| **Documentation only** | Clarification of existing semantics with no behavioral change | Rewording of attribute descriptions |
| **Constant not yet emitted** | New attribute defined upstream, but no OpenTelemetry* client in this repo populates a value for it | `gen_ai.usage.cache_creation.input_tokens` — defer the constant until a future PR adds an emission site |

> **No orphan constants.** A new constant in `OpenTelemetryConsts.cs` must only be added in a PR that also adds at least one emission site for it. If no client populates the attribute, classify the change as 🟢 *Constant not yet emitted* and defer adding the constant entirely — do not add it speculatively.

### 🟡 Minor Action Required

| Type | Description | Action |
|------|-------------|--------|
| **Version bump** | Convention version number changed | Update `v1.XX` in doc comments across all OpenTelemetry* files |
| **Stability promotion** | Attribute moved from experimental to stable | Usually no code change; note in audit table |

### 🔴 Code Change Required

| Type | Description | Action |
|------|-------------|--------|
| **New required attribute** | New attribute that should be emitted | Add constant, add emission code, add test assertion |
| **New metric** | New metric instrument defined | Add metric definition, emission, test |
| **Attribute rename** | Existing attribute renamed | Update constant value, verify backward compatibility |
| **New event** | New log/span event defined | Add event via `ILogger` + `[LoggerMessage]`, add test |
| **Behavioral change** | Change in how existing attributes are populated | Modify emission logic, update test expectations |
| **New operation name** | New `gen_ai.operation.name` value | Add detection logic, tests |
| **Schema change** | Change to JSON schema for serialized content (e.g. tool definitions) | Update serialization classes, `[JsonSerializable]` registration |

## Indicator Mapping

Use these indicators consistently in audit reports, implementation summaries, and PR descriptions:

| Indicator | Category | Meaning |
|---|---|---|
| 🟢 | No action required | No compensating code change is needed; explain why. |
| 🟡 | Minor action required | Small metadata, stability, or version-reference update. |
| 🔴 | Code change required | Runtime behavior, emission logic, metrics, events, serialization, or tests must change. |

## Impact Assessment Heuristic

For each gen-ai change in a release:

1. **Does it affect a capability we instrument?** Check the [file inventory](file-inventory.md) for matching client types.
   - No → classify as "N/A — No client exists"
2. **Is it already implemented?** Search `OpenTelemetryConsts.cs` for the attribute name.
   - Yes → classify as "Already implemented"
3. **Is it client-side or server-side?** Check the semantic convention's `span_kind` or context.
   - Server-side only → classify as "Server-side only"
4. **What kind of change is it?** Match to the categories above.
5. **How many files need modification?** Count affected files from the file inventory.
   - 1–2 files → Low complexity
   - 3–5 files → Medium complexity
   - 6+ files → High complexity (likely involves shared code or cross-cutting concern)

## Audit Table Format

When presenting the analysis, use this table format:

```markdown
| Semantic Convention Change | Upstream PR | Classification | Action Required | Complexity |
|---|---|---|---|---|
| `gen_ai.new.attribute` | [#1234](link) | New required attribute | Add constant + emission + test | Low |
| `gen_ai.deferred.attribute` | [#2345](link) | Constant not yet emitted | Defer — no client populates this attribute in this PR | — |
| `retrieval` operation | [#5678](link) | N/A — No client | None | — |
| Version reference | — | Version bump | Update doc comments | Low |
```

## PR Description Table Format

When preparing a PR description, adapt the audit table into a concise reviewer-facing table grouped or sorted by semantic-conventions version. Include every analyzed gen-ai change, not just changes that required code edits.

```markdown
| Version | Indicator | Semantic-conventions change | Classification | Compensating change / rationale |
|---|:---:|---|---|---|
| v1.XX | 🔴 | `gen_ai.new.attribute` added | New required attribute | Added constant, emission, and tests in `{files}`. |
| v1.XX | 🟡 | Version reference update | Version bump | Updated OpenTelemetry* doc comments to v1.XX. |
| v1.XX | 🟢 | Provider server span clarified | Server-side only | No client-side instrumentation change needed. |
| v1.XX | 🟢 | `gen_ai.deferred.attribute` added upstream | Constant not yet emitted | No client populates this attribute today; constant will be added in the PR that adds emission. |
```

The final column should either describe the compensating change made or explain why no code change was made, such as "already implemented", "no local source exists", "no client exists", "server-side only", "documentation-only clarification", or "no client populates this attribute today; constant deferred until a PR adds emission".
