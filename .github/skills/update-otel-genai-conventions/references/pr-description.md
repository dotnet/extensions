# PR Title and Description Format

When asked to create or update a PR after implementing GenAI semantic-conventions changes, use this guidance.

## Title

```text
Update OpenTelemetry GenAI conventions to v{version}
```

Use the target **GenAI** version for `{version}` — taken from the schema URL the new repo publishes (e.g. `opentelemetry.io/schemas/gen-ai/1.42.0` → `v1.42.0`) or from the CHANGELOG release header. The GenAI version is independent of core semconv (which has its own version line); do not conflate the two. If the PR also includes catch-up work from earlier convention versions or from the old `open-telemetry/semantic-conventions` repo, keep the title focused on the target GenAI version and explain the catch-up work in the description.

## Description

The description should include a changes table derived from the audit table and [change-classification.md](change-classification.md). Group or sort rows by GenAI version and include every analyzed change, not only the rows that produced code changes. Use the same red/yellow/green indicators as the classification guide:

- 🟢 for no action required
- 🟡 for minor action required
- 🔴 for code change required

Use this table shape (the **Area** column makes it obvious which package each row targets):

```markdown
| Version | Area | Indicator | Semantic-conventions change | Classification | Compensating change / rationale |
|---|---|:---:|---|---|---|
| vX.Y.Z | `gen-ai` | 🔴 | `gen_ai.example.attribute` added | New required attribute | Added constant, emission, and tests in `{files}`. |
| vX.Y.Z | `gen-ai` | 🟡 | Convention version reference changed | Version bump | Updated OpenTelemetry* doc comments. |
| vX.Y.Z | `gen-ai` | 🟢 | Server-side-only span attribute added | Server-side only | No client-side instrumentation change needed. |
| vX.Y.Z | `openai` | 🔴 | `openai.example.attribute` added | New required attribute | Added to `Microsoft.Extensions.AI.OpenAI` provider package. |
| vX.Y.Z | `mcp` | 🟢 | `mcp.example.attribute` added | N/A — No client | No MCP instrumentation in this repo today. |
```

For each row, describe the compensating change made, or explain why no change was made (already implemented, no local source, no client exists, server-side only, documentation only, etc.).

When the PR includes catch-up work whose source PRs live in the **old** `open-telemetry/semantic-conventions` repo (pre-migration `area:gen-ai` work), link to those old-repo PRs explicitly using the `open-telemetry/semantic-conventions#NNN` form so reviewers can disambiguate them from new-repo PR numbers.

Keep release-specific findings in the PR description or implementation summary; do not add them to the skill references unless they are durable cross-release guidance.
