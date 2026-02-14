# ApiChief Shorthand Aliases

When the user provides a library pattern, expand these shorthands before matching against `src/Libraries` folder names.

## Alias Table

Match aliases from **longest prefix first** to avoid ambiguous matches (e.g. `MEAI.Eval` before `MEAI`, `MEDI` before `MED` or `ME`).

| Shorthand prefix | Expands to |
|---|---|
| `MEAI.Eval` | `Microsoft.Extensions.AI.Evaluation` |
| `MEAI` | `Microsoft.Extensions.AI.*` (excluding MEAI.Eval) |
| `MESD` | `Microsoft.Extensions.ServiceDiscovery` |
| `MEDI` | `Microsoft.Extensions.DataIngestion` |
| `MED` | `Microsoft.Extensions.Diagnostics` |
| `MEH` | `Microsoft.Extensions.Http` |
| `MEC` | `Microsoft.Extensions.Compliance` |
| `MER` | `Microsoft.Extensions.Resilience` |
| `MET` | `Microsoft.Extensions.Telemetry` |
| `ME` | `Microsoft.Extensions.*` |
| `MAD` | `Microsoft.AspNetCore.Diagnostics.*` |
| `MA` | `Microsoft.AspNetCore.*` |

## Expansion Rules

1. Replace the shorthand prefix with the full name, preserving any suffix the user provided.
2. If the user appended `.` and more text, join naturally instead of using an inferred wildcard: `MEAI.OpenAI` → `Microsoft.Extensions.AI.OpenAI`.
3. If the user appended `*`, keep the wildcard: `MEAI*` → `Microsoft.Extensions.AI*`.
4. If the user did not prefix with `ME` or `MA`, use a 'contains' match that covers both middle and trailing segments: e.g. `OpenAI` → `*.OpenAI.*`, `*.OpenAI`, or `*.OpenAI*`, and `DependencyInjection` → `*.DependencyInjection.*`, `*.DependencyInjection`, or `*.DependencyInjection*`.
5. All alias and expanded matching is case-insensitive.

## Subset Exclusion Rules

When one alias's expansion is a prefix of another's, the **more specific** alias's libraries are excluded from wildcard matches of the **broader** alias — unless the user explicitly names the more specific set. This prevents broad wildcards from sweeping in specialized library groups that the user likely didn't intend.

For example, `MEAI` expands to `Microsoft.Extensions.AI` and `MEAI.Eval` expands to `Microsoft.Extensions.AI.Evaluation`. Because `Evaluation` is a subset prefix of `AI`, the Evaluation libraries are excluded from `MEAI*` unless the user explicitly requests them (e.g. `MEAI.Eval*`).
