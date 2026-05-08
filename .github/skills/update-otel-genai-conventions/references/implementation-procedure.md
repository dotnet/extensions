# Implementation Procedure

Used by Modes 2 (Autopilot), 4 (CCA Implementation), and 5 (Plan-then-Implement) when actually applying convention changes.

1. Read [implementation-patterns.md](implementation-patterns.md) and [testing-guide.md](testing-guide.md)
2. Read [review-checklist.md](review-checklist.md) to anticipate review feedback
3. Apply changes in this order:
   - Add new constants to `OpenTelemetryConsts.cs` **only for attributes whose emission is also added in this same PR**. Do not add constants speculatively — if no OpenTelemetry* client in this repo will populate the attribute, defer the constant until the PR that wires up emission and classify the change as 🟢 *Constant not yet emitted* per [change-classification.md](change-classification.md).
   - **Choose the correct constant location based on the current repo layout**: use the shared `Microsoft.Extensions.AI/OpenTelemetryConsts.cs` nested classes for shared upstream areas such as `GenAI.*` (`gen-ai/*`) and, when added, `MCP.*` (`mcp/*`). Do **not** assume there is already an `OpenAI.*` nested class there. For provider-specific attributes (`openai.*`, `anthropic.*`, `aws-bedrock.*`, `azure-ai-inference.*`), follow the provider package's existing layout. For example, OpenAI provider-specific tag names are currently defined as `internal const string` values in `OpenAIClientExtensions.cs`. If a provider accumulates enough shared constants to justify a dedicated provider constants file/class, introduce that file/class explicitly in the same PR rather than assuming one already exists.
   - **Before adding any new helper, method, or internal type**, search `Common/`, `TelemetryHelpers.cs`, `OpenTelemetryLog.cs`, and the sibling OpenTelemetry* client files for existing logic with the same purpose. Reuse or extend rather than introducing a parallel implementation. If the same logic is needed in two or more places, factor it into `Common/` from the start instead of duplicating it per file. The same applies to parallel internal types — unify types with identical shape under a single shared definition.
   - Add attribute/metric emission to the relevant OpenTelemetry* client classes
   - Update version references in doc comments across all files that reference the convention version (see [file-inventory.md §Version References](file-inventory.md#version-references) for the wording-migration guidance)
   - Update or augment tests
4. Self-review against [review-checklist.md](review-checklist.md)
5. Validate per the **Validation** section in `SKILL.md`
