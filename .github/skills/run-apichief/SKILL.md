---
description: 'Run ApiChief against libraries matching a pattern to emit baselines, summaries, deltas, reviews, or check for breaking changes'
agent: 'agent'
tools: ['github/*']
---

# Run ApiChief

Run the [ApiChief tool](../../../eng/Tools/ApiChief/README.md) against one or more libraries in `src/Libraries`.

## Shorthand Aliases

Expand shorthands **before** matching. See the [alias table](references/apichief-aliases.md).

## Subset Exclusion Rules

When one shorthand's expansion is a prefix of another's, the more specific libraries are **excluded** from broad wildcard matches unless explicitly requested. See the [subset exclusion rules](references/apichief-aliases.md#subset-exclusion-rules) for the full list. For example, `MEAI*` excludes `Evaluation*` libraries, and `MED*` excludes `DataIngestion*` libraries.

## Steps

Follow the detailed [execution steps](references/apichief-steps.md).
