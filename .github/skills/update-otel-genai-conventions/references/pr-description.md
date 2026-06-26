# PR Title and Description Format

When asked to create or update a PR after implementing GenAI semantic-conventions changes, use this guidance.

## Title

When a target **GenAI** version number is known (a release or published schema URL exists):

```text
Update OpenTelemetry GenAI conventions to v{version}
```

When no GenAI version number is determined yet -- the common case while `semantic-conventions-genai` is unreleased (no release and no published schema URL) -- use `latest` in place of a version number:

```text
Update OpenTelemetry GenAI conventions to latest
```

The GenAI version is independent of core semconv (which has its own version line); do not conflate the two, and do not use `versions.env`'s `SEMCONV_VERSION` (that is the core semconv dependency) as the title version. When a version exists, take `{version}` from the `CHANGELOG.md` release header (Towncrier compiles the `changelog.d/` fragments into it at release time) or the published schema URL (`opentelemetry.io/schemas/gen-ai/X.Y.Z`). While unreleased, keep the title as `latest` and identify the update by its `changelog.d/` fragment snapshot (commit / ref / date) in the body instead of a version number. If the PR also includes catch-up work from earlier convention versions or from the consolidated `open-telemetry/semantic-conventions` repo, keep the title focused on the target (the GenAI version or `latest`) and explain the catch-up work in the description.

## Description

The description should include a changes table derived from the audit table and [change-classification.md](change-classification.md). Group or sort rows by GenAI version and include every analyzed change, not only the rows that produced code changes. When no GenAI version number is determined yet, use `latest` in the **Version** column and record the `changelog.d/` fragment snapshot (commit / ref / date) you audited in the description. Use the same red/yellow/green indicators as the classification guide:

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

When the PR includes catch-up work whose source PRs live in the consolidated `open-telemetry/semantic-conventions` repo (earlier `area:gen-ai` work, before these conventions moved), link to those PRs explicitly using the `open-telemetry/semantic-conventions#NNN` form so reviewers can disambiguate them from `semantic-conventions-genai` PR numbers.

### Upstream scan tracking tables

A recurring **upstream-scan tracking PR** (the kind that carries the `otel-genai-tracking` state block) lists every merged `Unreleased` change and every open upstream PR, each with its applicability to this repo. Use **one consistent column set for both the merged-changes table and the in-flight (open-PR) table** so they read the same way:

```markdown
| Upstream PR | Area | Change | Applicability | Status |
|---|---|---|:---:|---|
| [#NNN](https://github.com/open-telemetry/semantic-conventions-genai/pull/NNN) | `gen-ai` | `gen_ai.example.attribute` | 🔴 | Implemented in `OpenTelemetryChatClient`. |
| [#NNN](https://github.com/open-telemetry/semantic-conventions-genai/pull/NNN) | `gen-ai` | `top_k` type change | ✅ | Already aligned (`ChatOptions.TopK` is `int?`). |
| [#NNN](https://github.com/open-telemetry/semantic-conventions-genai/pull/NNN) | `gen-ai` | `document` modality | 🟡 | Watch (additive message serialization). |
| [#NNN](https://github.com/open-telemetry/semantic-conventions-genai/pull/NNN) | `mcp` | tool.call.arguments opt-in | 🟢 | No MCP instrumentation. |
```

Keep the **Applicability** column to the color symbol only, and put the explanatory text in the separate **Status** column. The merged-changes table and the in-flight (open-PR) table share these columns exactly; in the in-flight table, **Status** describes what would change *if the PR merged*.

Keep the **Change** text wrappable so the table fits the screen: browsers do not break long `/`-separated runs (e.g. `entity/identity/finish_reason/...`), and such a run forces the column -- and the whole table -- wider than the viewport. Write multi-segment descriptions with break opportunities (`', '` or `' / '` with surrounding spaces) so the cell can wrap.

Applicability legend (symbol-only in the Applicability column):

- 🔴 implemented here
- ✅ already aligned (no change needed)
- 🟡 watch / deferred
- 🟢 not applicable (no client / docs-only / other repo)

Keep release-specific findings in the PR description or implementation summary; do not add them to the skill references unless they are durable cross-release guidance.
