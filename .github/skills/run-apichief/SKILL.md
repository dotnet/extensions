---
description: 'Run ApiChief against libraries matching a pattern to emit baselines, summaries, deltas, reviews, or check for breaking changes'
agent: 'agent'
tools: ['github/*']
---

# Run ApiChief

Run the [ApiChief tool](../../../eng/Tools/ApiChief/README.md) against one or more libraries in `src/Libraries`.

The user might provide a shorthand or abbreviated reference to the library or libraries in scope (e.g. "MEAI" for `Microsoft.Extensions.AI`). Match their intent against the folder names in `src/Libraries/`.

## Steps

Follow the detailed [execution steps](references/apichief-steps.md).
