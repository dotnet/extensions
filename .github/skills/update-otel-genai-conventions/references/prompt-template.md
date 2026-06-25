# CCA Prompt Template

Template for generating a structured prompt suitable for delegating convention update work to Copilot Coding Agent (CCA) on github.com.

## Template

Fill in the bracketed sections based on the analysis of the semantic-conventions release.

---

```markdown
## Background

The OpenTelemetry semantic conventions {VERSION} release includes gen-ai changes that require compensating updates in dotnet/extensions. Release notes: {RELEASE_URL}

Key upstream PRs:
{FOR_EACH_UPSTREAM_PR}
- [{PR_TITLE}]({PR_URL})
{END_FOR_EACH}

## Changes Audit

| Semantic Convention Change | Upstream PR | Classification | Action Required |
|---|---|---|---|
{FOR_EACH_CHANGE}
| `{ATTRIBUTE_OR_CHANGE_NAME}` | [#{PR_NUMBER}]({PR_URL}) | {CLASSIFICATION} | {ACTION} |
{END_FOR_EACH}

## Required Changes

### 1. Version References

Update the semantic conventions version reference from `v{OLD_VERSION}` to `v{NEW_VERSION}` in doc comments across ALL OpenTelemetry* client files:

{LIST_ALL_FILES_WITH_VERSION_REFERENCE}

The doc comment pattern to update:
```csharp
/// Semantic Conventions for Generative AI systems v{OLD_VERSION},
```
→
```csharp
/// Semantic Conventions for Generative AI systems v{NEW_VERSION},
```

### 2. New Constants

Add these constants to `src/Libraries/Microsoft.Extensions.AI/OpenTelemetryConsts.cs`:

{FOR_EACH_NEW_CONSTANT}
In the `{PARENT_CLASS}` nested class:
```csharp
public const string {CONSTANT_NAME} = "{ATTRIBUTE_NAME}";
```
{END_FOR_EACH}

### 3. Attribute Emission

{FOR_EACH_NEW_ATTRIBUTE}
#### 3.{N}. `{ATTRIBUTE_NAME}` in `{CLIENT_FILE}`

{DESCRIPTION_OF_WHERE_AND_HOW_TO_EMIT}

Current code context (around line {LINE_NUMBER}):
```csharp
{EXISTING_CODE_SNIPPET}
```

Add:
```csharp
{NEW_CODE_TO_ADD}
```
{END_FOR_EACH}

### 4. Tests

Update tests in `{TEST_FILE_PATH}`:

{FOR_EACH_TEST_UPDATE}
- {EXISTING_OR_NEW}: {DESCRIPTION_OF_ASSERTION_TO_ADD}
{END_FOR_EACH}

Reference the `update-otel-genai-conventions` skill in `.github/skills/` for:
- Implementation patterns in `references/implementation-patterns.md`
- Testing guide in `references/testing-guide.md`
- Review checklist in `references/review-checklist.md`

## Validation

After implementing changes:
1. Restore, generate the AI-filtered solution, build, and run the tests using the Linux/macOS commands in `.github/skills/update-otel-genai-conventions/references/build-commands.md`
2. If the public API surface changed, run `pwsh ./scripts/MakeApiBaselines.ps1` and keep only the baselines for the libraries actually changed
3. Verify no remaining references to the old version: `grep -rn "v{OLD_VERSION}" src/Libraries/Microsoft.Extensions.AI/`
```

---

## Prompt Quality Guidelines

Based on analysis of successful CCA prompts (PRs #7379, #7382, #7322):

### What makes a good prompt

1. **Exact file paths** — always include full relative paths from repo root
2. **Current code context** — show the existing code around the modification point with line numbers
3. **Expected code** — show what the new code should look like
4. **Constant values** — specify the exact string values for new OTel attribute names
5. **Test expectations** — specify which test file and whether to augment existing tests or create new ones
6. **Validation commands** — include the build/test commands to run

### What to avoid

1. **Vague instructions** — "update the tests" → specify exactly which assertions to add
2. **Missing files** — forgetting to update version references in all OpenTelemetry* files
3. **Wrong approach** — specifying `Activity.AddEvent` when `ILogger` should be used for events
4. **Incomplete scope** — only covering chat client when embedding generator also needs changes

### Prompt size guidance

- **Simple version bump** (few code changes): ~1,000–2,000 characters
- **New attributes/metrics** (moderate changes): ~3,000–5,000 characters
- **Behavioral changes** (complex): ~5,000–8,000 characters
- **Audit table only** (version bump with analysis): use the concise audit table format from PR #7322
