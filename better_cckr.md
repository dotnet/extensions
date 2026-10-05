# Better Bottom-K configuration

## Scope

This change makes bottom-K sampling configurable through the .NET options and dependency-injection patterns used by R9. It adds safe bypass policies, configuration binding, validation, live option reads, and benchmark coverage without rebuilding a NuGet package.

## Implemented work

1. Added three registration paths:
   - `AddBottomKLogSampling()` for defaults.
   - `AddBottomKLogSampling(Action<BottomKLogSamplingOptions>)` for code configuration.
   - `AddBottomKLogSampling(IConfigurationSection)` for JSON and other configuration providers.
2. Registered `BottomKLogSamplingOptions` through `AddOptionsWithValidateOnStart`.
3. Injected `IOptionsMonitor<BottomKLogSamplingOptions>` into the bottom-K buffer so configuration-provider reloads are observed without rebuilding the service provider.
4. Preserved the single shared `BottomKLogBuffer` instance behind both `LoggingSampler` and `LogBuffer`.
5. Added explicit registration rejection when another log buffer already owns the single logging buffer seam, rather than silently disabling bottom-K sampling.
6. Added validation for:
   - positive capacity;
   - non-negative novelty-preserve capacity;
   - non-negative minimum period count;
   - positive flush interval;
   - valid unseen-weight mode;
   - valid retain-all log levels;
   - non-empty category patterns;
   - at most one wildcard per category pattern.
7. Added the `Enabled` kill switch. Disabled bottom-K sampling bypasses the reservoir and sends records to ordinary logging providers.
8. Added `RetainAllLogLevels`. It defaults to `Error` and `Critical`.
9. Added `RetainAllCategories`. Matching records bypass bottom-K sampling and remain unchanged.
10. Added `RetainAllEventIds`. This supports explicit protection for audit, security, control-plane, and LongTermRetention event contracts when they have stable event identifiers.
11. Added `SampledCategories`. An empty list samples all otherwise eligible categories; a non-empty list acts as an allowlist, and categories outside it are retained normally.
12. Added case-insensitive category matching with one optional `*` wildcard.
13. Made retain-all policies bypass bottom-K buffering as well as admission. This avoids duplicate records and preserves normal provider routing.
14. Applied live policy changes on the next log operation.
15. Applied live algorithm changes by flushing retained records under the prior configuration and rebuilding each affected category reservoir before its next admission.
16. Made flush-interval changes effective without recreating the service provider.
17. Added generation-aware admission handling so a period flush or configuration change between sampling and buffering cannot insert an old admission into a new reservoir.
18. Moved automatic period checks to the sampling path so rejected traffic can still trigger period boundaries.
19. Made disposal flush retained records without making subsequent shutdown-time logging throw from disposed thread-local state.
20. Kept `sampling.count` before `{OriginalFormat}` in emitted structured state.
21. Returned emitted or invalidated serialized records to the shared pool.
22. Extended `BottomKImpactBench` with:
   - `BottomKDisabled`;
   - `BottomKRetainAllPolicy`.
23. Updated the benchmark design document to explain the two new rollout-policy measurements.
24. Added `BottomKLogLevelPolicyBench` to isolate the incremental cost of passing `LogLevel` into bottom-K sampling and checking non-matching `Error` and `Critical` retain-all policies.
25. Added `BottomKCategoryPolicyBench` to isolate two non-matching retain-all category wildcard checks.
26. Added `BottomKEventIdPolicyBench` to isolate two non-matching retain-all EventId checks.
27. Removed per-record category-policy enumerator allocation discovered by the benchmark by using indexed `IList<string>` access.

## Log-level policy benchmark result

The benchmark ran on .NET 10.0.12 in a Hyper-V Windows 11 VM with tiered compilation disabled. Every input record was `Information`, so the `Error` and `Critical` retain-all policies did not bypass bottom-K sampling. Both cases therefore performed the same adaptive admission and buffering work.

| Records | No log-level policy | Error/Critical policy miss | Ratio | Approximate delta per record |
| ---: | ---: | ---: | ---: | ---: |
| 10,000 | 3.798 ms | 3.779 ms | 1.00 | -1.9 ns |
| 20,000 | 6.584 ms | 6.727 ms | 1.03 | +7.2 ns |

The confidence intervals overlap in both comparisons. The benchmark did not detect a statistically meaningful CPU cost from passing `LogLevel` and scanning two configured values. It also found no consistent allocation increase. The practical conclusion is that this policy check is below the noise floor of the full bottom-K logging path on this VM; it should not be presented as a demonstrated 3% regression.

The generated report is at:

`C:\dotnet_extensions\BenchmarkDotNet.Artifacts\results\Microsoft.Extensions.Telemetry.Bench.BottomKLogLevelPolicyBench-report-github.md`

## Category policy benchmark result

The comparison checked two non-matching wildcard patterns for every record. Both pipelines performed the same adaptive bottom-K work.

| Records | No category policy | Two category policy misses | Ratio | Approximate delta per record |
| ---: | ---: | ---: | ---: | ---: |
| 10,000 | 3.404 ms | 4.198 ms | 1.23 | 79.4 ns |
| 20,000 | 6.181 ms | 7.418 ms | 1.20 | 61.9 ns |

The category checks added approximately 20–23% to this in-memory bottom-K benchmark. This is the cost of two case-insensitive wildcard comparisons per record, not category extraction: the category string already exists on `LogEntry`.

The first run also found a boxed `IList<string>` enumerator allocation per policy evaluation. Indexed access removed that allocation before the final run. The final benchmark shows no material policy-related allocation increase.

The generated report is at:

`C:\dotnet_extensions\BenchmarkDotNet.Artifacts\results\Microsoft.Extensions.Telemetry.Bench.BottomKCategoryPolicyBench-report-github.md`

## EventId policy benchmark result

The comparison checked two non-matching retained EventIds for every record. Workload EventIds were 1 through 16; protected identifiers were 10001 and 10002.

| Records | No EventId policy | Two EventId policy misses | Ratio | Approximate delta per record |
| ---: | ---: | ---: | ---: | ---: |
| 10,000 | 3.450 ms | 3.689 ms | 1.07 | 23.9 ns |
| 20,000 | 6.054 ms | 6.236 ms | 1.03 | 9.1 ns |

EventId checks added 3–7% in these runs with no allocation increase. The 20,000-record confidence intervals overlap, so the exact cost is noisy; the evidence supports a small CPU cost substantially below category wildcard matching.

The generated report is at:

`C:\dotnet_extensions\BenchmarkDotNet.Artifacts\results\Microsoft.Extensions.Telemetry.Bench.BottomKEventIdPolicyBench-report-github.md`

## Configuration example

```json
{
  "Logging": {
    "BottomK": {
      "Enabled": true,
      "Capacity": 128,
      "PreserveCapacity": 128,
      "MinPeriodCount": 32,
      "FlushInterval": "00:00:30",
      "UnseenWeightMode": "Chao1",
      "RetainAllLogLevels": [
        "Error",
        "Critical"
      ],
      "RetainAllCategories": [
        "Microsoft.Hosting.Lifetime",
        "Contoso.Security.*",
        "Contoso.Audit.*"
      ],
      "RetainAllEventIds": [
        1001,
        1002
      ],
      "SampledCategories": [
        "Contoso.Requests.*",
        "Contoso.Diagnostics.*"
      ]
    }
  }
}
```

```csharp
services.AddLogging(builder =>
{
    builder.AddBottomKLogSampling(
        configuration.GetSection("Logging:BottomK"));
});
```

## Policy precedence

For each log record, bottom-K sampling applies the following order:

1. If `Enabled` is `false`, retain normally.
2. If the log level appears in `RetainAllLogLevels`, retain normally.
3. If the event ID appears in `RetainAllEventIds`, retain normally.
4. If the category matches `RetainAllCategories`, retain normally.
5. If `SampledCategories` is non-empty and the category does not match it, retain normally.
6. Otherwise, apply bottom-K admission and buffering.

## Deliberately not included

- A global record or byte budget is not represented as an option because the current bottom-K algorithm allocates capacity per category. A correct global budget requires a separate allocator rather than a configuration-only change.
- Shadow mode is not exposed because the current implementation has no trustworthy would-keep/would-drop metrics. A flag without observable evaluation data would not support a safe rollout.
- Configuration changes preserve already-retained records by flushing them before an algorithm reset; they do not silently discard buffered telemetry.
- Bottom-K sampling cannot be combined with global or per-request log buffering in the same pipeline because the .NET logging stack exposes one `LogBuffer` slot.
- Unit tests cover the estimator, K+1 threshold behavior, payload ownership, policies, configuration changes, and lifecycle behavior.
- No NuGet package was built.
