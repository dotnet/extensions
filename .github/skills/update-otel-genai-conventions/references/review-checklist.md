# Review Checklist

Review checklist for gen-ai convention changes. Based on patterns from past PR reviews by domain experts (@stephentoub, @tarekgh, @lmolkova, @CodeBlanch).

## Critical Checks

### 1. Exception Recording Approach
- [ ] Exception events use `ILogger` + `[LoggerMessage]`, NOT `Activity.AddEvent`
- [ ] Log message definitions are in `Common/OpenTelemetryLog.cs`
- [ ] `[LoggerMessage]` message text matches the OTel event name

**Past feedback**: PR #7379 — tarekgh and CodeBlanch directed change from `Activity.AddEvent` to `ILogger`-based approach per OTel migration plan.

### 2. Sensitive Data Gating
- [ ] Attributes that could contain user data are gated behind `EnableSensitiveData`
- [ ] `exception.message` is treated as potentially sensitive
- [ ] Message content serialization respects the sensitive data setting
- [ ] Test coverage for both `EnableSensitiveData = true` and `false`

**Past feedback**: PR #7379 — stephentoub raised whether `exception.message` should be guarded.

### 3. Code Deduplication
- [ ] Cross-cutting telemetry code is shared via `Common/` classes, not duplicated
- [ ] Similar patterns across multiple OpenTelemetry* clients use shared helpers
- [ ] New helper methods are added to `TelemetryHelpers.cs` or `OpenTelemetryLog.cs` as appropriate
- [ ] **Search before adding**: before introducing a new helper, method, or internal type, search `Common/`, `TelemetryHelpers.cs`, `OpenTelemetryLog.cs`, and sibling OpenTelemetry* client files for existing logic that does the same thing. Prefer reusing an existing helper or extending it over adding a parallel one.
- [ ] **Cross-file diff for new helpers**: when the same convention change introduces helper logic in more than one OpenTelemetry* client, diff the new blocks against each other. Byte-for-byte (or near-byte-for-byte) identical helpers must be factored into a shared helper in `Common/` rather than duplicated per file.
- [ ] **Parallel types with identical shape**: when defining a new internal/private type (e.g. for serialization, OTel mapping, tool definitions), check whether a sibling client already defines a type with the same shape (same properties, same purpose). Unify the two — either reuse the existing type or move both to a single shared definition under `Common/`.

**Past feedback**:
- PR #7379 — tarekgh noted duplicated code across clients and requested consolidation to `Common/`.
- PR [#7497 r3161304243](https://github.com/dotnet/extensions/pull/7497#discussion_r3161304243) — reviewer flagged that the `chat` / `chat {name}` activity-name check was duplicated in several files; consolidated to a shared helper.
- PR [#7497 r3161364739](https://github.com/dotnet/extensions/pull/7497#discussion_r3161364739) / [r3162514449](https://github.com/dotnet/extensions/pull/7497#discussion_r3162514449) — reviewer flagged that `CreateOtelToolDefinition` returned a `RealtimeOtelFunction` in the realtime client and an `OtelFunction` in the chat client, with byte-for-byte identical logic and identical type shape (`Name`, `Description`, `Parameters`, `Type`). The two parallel types should have been unified from the start.

### 4. Fluent API Style
- [ ] Activity API calls use fluent chains (`.SetStatus(...).SetTag(...)`)
- [ ] No separate statement for each Activity method call

**Past feedback**: PR #7379 — stephentoub requested fluent chain continuation.

### 5. Test Organization
- [ ] Existing tests augmented with new assertions rather than creating new test methods where possible
- [ ] Both streaming and non-streaming paths tested
- [ ] Sensitive data gating tested (both enabled and disabled)
- [ ] Missing/default value behavior tested

**Past feedback**: PR #7379 — stephentoub asked "do we already have tests validating error.type? If so, can you just augment those".

### 6. Version Reference Completeness
- [ ] All files with a gen-ai semantic conventions version reference use the same version before starting the update
- [ ] ALL OpenTelemetry* client files with a version reference have that reference updated
- [ ] Grep confirms no remaining references to the old version: `grep -rn "v1.OLD" src/Libraries/Microsoft.Extensions.AI/`

### 7. Constants Organization
- [ ] New constants added to appropriate nested class in `OpenTelemetryConsts.cs`
- [ ] Constant names follow PascalCase convention
- [ ] String values match the semantic convention attribute names exactly
- [ ] **No orphan constants**: every newly added constant in `OpenTelemetryConsts.cs` is referenced by at least one emission site added in this PR. Verify with `grep -rn NewConstantName src/Libraries/Microsoft.Extensions.AI/`. If no client populates the attribute, the constant must be removed from this PR and deferred to the PR that adds emission (classify as 🟢 *Constant not yet emitted*).

### 8. Scope Completeness
- [ ] Changes applied to ALL relevant OpenTelemetry* client classes (not just the chat client)
- [ ] If a change affects embeddings, image generation, speech, etc., those clients are also updated
- [ ] Function invocation changes apply to both `FunctionInvokingChatClient` and shared `Common/FunctionInvocationProcessor.cs`
- [ ] Realtime function invocation via `FunctionInvokingRealtimeClientSession` is also covered if applicable

**Past feedback**: PR #7379 — stephentoub asked to extend changes to additional client types.

### 9. JSON Serialization
- [ ] New content part types have proper inner classes
- [ ] `[JsonSerializable]` registration added to `OtelContext`
- [ ] Switch case added in `SerializeChatMessages()` for new types

### 10. Metric Alignment
- [ ] New metrics have proper instrument creation (Histogram, Counter, etc.)
- [ ] Metric units use constants (`SecondsUnit`, `TokensUnit`)
- [ ] Metric tags align with span attributes where applicable

## Common Mistakes

| Mistake | Correct Approach |
|---------|-----------------|
| Using `Activity.AddEvent` for exceptions | Use `ILogger` + `[LoggerMessage]` |
| Separate Activity API statements | Use fluent chains |
| Creating new test methods for existing scenarios | Augment existing test assertions |
| Only updating `OpenTelemetryChatClient` | Update ALL relevant OpenTelemetry* clients |
| Missing `EnableSensitiveData` gate | Gate any attribute with user-generated content |
| Updating version in one file only | Check for version drift first, then update ALL files with version reference |
| Creating CHANGELOG entries | No CHANGELOGs — info goes in release notes only |
| Using `null` for optional metric units | Use the appropriate unit constant or omit |
| Adding a constant for an attribute no client emits | Defer the constant until the PR that adds the emission site (classify as 🟢 *Constant not yet emitted*) |
| Adding a new helper without searching for an existing one | Search `Common/`, `TelemetryHelpers.cs`, `OpenTelemetryLog.cs`, and sibling OpenTelemetry* clients first; reuse or extend rather than parallel-implement |
| Defining a parallel internal type with the same shape as one in a sibling client (e.g. `RealtimeOtelFunction` vs `OtelFunction`) | Unify the types — reuse the existing one or move a single shared definition to `Common/` |
