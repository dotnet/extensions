# PR Title and Description Format

When asked to create or update a PR after implementing semantic-conventions changes, use this guidance.

## Title

```text
Update OpenTelemetry gen-ai conventions to v{version}
```

Use the target semantic-conventions release version for `{version}`. If the PR also includes catch-up work from earlier releases, keep the title focused on the target version and explain the catch-up work in the description.

## Description

The description should include a changes table derived from the audit table and [change-classification.md](change-classification.md). Group or sort rows by semantic-conventions version and include every analyzed gen-ai change, not only the rows that produced code changes. Use the same red/yellow/green indicators as the classification guide:

- 🟢 for no action required
- 🟡 for minor action required
- 🔴 for code change required

Use this table shape:

```markdown
| Version | Indicator | Semantic-conventions change | Classification | Compensating change / rationale |
|---|:---:|---|---|---|
| v1.XX | 🔴 | `gen_ai.example.attribute` added | New required attribute | Added constant, emission, and tests in `{files}`. |
| v1.XX | 🟡 | Convention version reference changed | Version bump | Updated OpenTelemetry* doc comments. |
| v1.XX | 🟢 | Server-side-only span attribute added | Server-side only | No client-side instrumentation change needed. |
```

For each row, describe the compensating change made, or explain why no change was made (already implemented, no local source, no client exists, server-side only, documentation only, etc.).

Keep release-specific findings in the PR description or implementation summary; do not add them to the skill references unless they are durable cross-release guidance.
