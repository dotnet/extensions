# Experimental decision abstractions

The `Microsoft.Extensions.AI.Abstractions` decision contracts provide a small provider-neutral Layer 1 capability for heterogeneous binary, choice, and ordinal-score questions. Requests preserve caller-owned question and candidate IDs and order; responses preserve complete probability observations, score precision, provenance, usage, and provider extensions without applying thresholds, calibration, or probability repair.

Typed application results use an explicit `JsonTypeInfo<T>` and mapper supplied by the application. Feature projection is likewise explicit: callers define a versioned schema of named `double` coordinates over the complete observations. The contracts snapshot collections and JSON state; `DecisionOptions.Clone` deliberately snapshots provider extension data before a request is shared.

This layer intentionally does not define tools, routing composition, MEDI processors, provider adapters, ML.NET or Arrow integrations, or automatic POCO/union inference. Those concerns require a later layer and are not implied by these abstractions.
