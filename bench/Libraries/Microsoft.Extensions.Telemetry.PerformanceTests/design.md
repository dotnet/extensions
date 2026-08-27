# Sampling and buffering performance tests

## Goal

Measure the incremental CPU and managed-memory cost of log sampling and buffering relative to the same logging pipeline without either feature. The sampling comparison must also show when dropping logs offsets the sampler's own cost.

BenchmarkDotNet mean time per operation is used as the CPU-cost proxy. `MemoryDiagnoser` reports managed allocations and garbage collections per operation. These measurements intentionally do not represent process working set or machine-wide CPU utilization.

Each benchmark invocation processes either 10,000 or 20,000 logs, representing one minute of traffic at the selected rate. BenchmarkDotNet reports time and allocation for the complete one-minute batch. Divide those values by `RecordsPerMinute` when a per-log value is needed.

The shared deterministic workload uses four categories and sixteen event IDs. Event frequency is intentionally skewed: event 1 accounts for 70% of records, event 2 for 15%, event 3 for 8%, and the remaining 7% is spread across events 4 through 16. This exercises category/event rule caches and gives CCKR both frequent and rare callsites.

The benchmarks process the minute's traffic as a batch rather than sleeping between logs. This keeps wall-clock waiting out of the measurements and isolates logging pipeline cost.

## Sampling benchmark

`SamplingImpactBench` builds two otherwise identical logging pipelines:

- `NoSampling` is the baseline and sends every log to `BenchLogger`.
- `WithSampling` adds one sampler before the same provider.

Both methods use a cached `LoggerMessage` delegate with one primitive structured argument. This avoids call-site formatting and boxing allocations while still exercising the structured log state that passes through the sampling pipeline, making sampler-introduced allocations visible.

The benchmark runs the following scenarios:

| Scenario | Configuration | Purpose |
| --- | --- | --- |
| `RandomSampleAll` | Probability `1.0` | Measures random sampler overhead when provider work is unchanged. |
| `RandomSampleOnePercent` | Probability `0.01` | Represents a high-volume production configuration where most logs are discarded. |
| `RandomDropAll` | Probability `0.0` | Establishes the lower bound when all provider work is avoided. |
| `RandomByCategory` | High-volume categories `0.01`, critical categories `1.0`, fallback `0.1` | Measures category-rule selection with different retention policies. |
| `TraceSample` | Current activity has the `Recorded` flag | Measures trace-based sampler overhead when the log is retained. |
| `TraceDrop` | Current activity is not recorded | Measures trace-based sampling when provider work is avoided. |

Each scenario is a separate BenchmarkDotNet parameter, so its `WithSampling` result is compared directly with a `NoSampling` baseline created under the same process and job settings. Setup, dependency injection, logger creation, and activity creation are outside the measured operations.

## Buffering benchmark

`BufferingImpactBench` builds two otherwise identical logging pipelines:

- `NoBuffering` is the baseline and sends the selected record count directly to `BenchLogger`.
- `BufferOnly` measures serialization and insertion of the selected record count into the global buffer. Its flush runs during iteration cleanup and is excluded from the measurement.
- `BufferAndFlush` measures the end-to-end cost of buffering and then emitting the selected record count to `BenchLogger`.

The buffer is sized to retain all batches from a measured iteration and automatic post-flush bypass is disabled. `BufferOnly` is flushed after each benchmark iteration, outside the measurement. This avoids capacity eviction and ensures every measured iteration starts with an empty buffer.

## CCKR benchmark

`CckrImpactBench` is available on the CCKR integration branch. CCKR combines the sampling decision and reservoir buffering in one pipeline, so its decision and buffer costs cannot be isolated through the public logging integration.

- `NoSampling` is the baseline and sends every log directly to `BenchLogger`.
- `CckrRetainAll` gives the reservoir enough capacity for the selected record count and measures admission plus buffering with no drops.
- `CckrRetainAllAndFlush` adds emission of every retained log, making downstream provider work equivalent to the baseline.
- `CckrAdaptive` uses a representative fixed capacity of 128 per category (up to 512 records across the four-category workload) for the selected one-minute period and measures the adaptive high-volume path without flush cost.
- `CckrAdaptiveAndFlush` includes emission of the adaptive reservoir at the period boundary.

The novelty preserve is disabled so retained records are controlled only by the configured reservoir capacity. Automatic time-based flushing is moved beyond the benchmark duration, and iteration cleanup flushes both reservoirs outside the measurement. CCKR uses random ranks, so adaptive results should be interpreted from the full BenchmarkDotNet run rather than a single invocation.

## Running

From the repository root:

```powershell
dotnet run -c Release --project .\bench\Libraries\Microsoft.Extensions.Telemetry.PerformanceTests\Microsoft.Extensions.Telemetry.PerformanceTests.csproj -- --filter *SamplingImpactBench* *BufferingImpactBench* *CckrImpactBench*
```

Run on an otherwise idle machine with a fixed power plan. Compare `Mean`, `Ratio`, `Allocated`, and GC columns. Retain the generated BenchmarkDotNet artifacts with the machine, OS, runtime, and processor metadata when comparing changes over time.

## Interpretation

- `RandomSampleAll` and `TraceSample` isolate the cost of making a sampling decision because both paths still invoke the provider.
- `RandomDropAll` and `TraceDrop` show the best-case savings when sampling bypasses provider processing.
- `RandomSampleOnePercent` captures the combined decision cost and expected provider savings, but individual invocations are nondeterministic. BenchmarkDotNet's repeated operations provide the aggregate result.
- `RandomByCategory` retains approximately 28% of this evenly distributed four-category workload: 1% for two high-volume categories, 100% for the critical category, and 10% for the fallback category.
- Results depend on provider cost. `BenchLogger` is deliberately lightweight, so dropped-path savings are conservative relative to providers that format, serialize, buffer, or export logs.
- `BufferOnly` isolates the cost and managed allocations required to retain logs in memory.
- `BufferAndFlush` includes deserialization and downstream provider work, so it represents the complete buffering lifecycle.
- `CckrRetainAll` shows the combined admission and buffering overhead when sampling provides no volume reduction.
- `CckrAdaptive` shows when dropped-log savings offset reservoir decision cost, while the `AndFlush` variants include the cost of emitting retained records.

## Follow-up plan

1. Record a baseline on each supported performance-test platform.
2. Track accepted-path CPU and allocation regressions separately from dropped-path throughput gains.
3. Add a representative production exporter benchmark only if end-to-end exporter savings are needed; keep it separate so I/O and serialization do not hide sampler regressions.
4. Add multithreaded contention coverage if rule-cache or random-number generation changes, because this initial benchmark isolates steady-state single-thread overhead.

## Acceptance criteria

- Every sampling implementation has both a retained and discarded-log scenario.
- Each sampling result has an equivalent no-sampling baseline.
- Buffer insertion and buffer insertion plus flush are both compared with direct logging.
- CCKR covers retain-all and adaptive-drop paths, both before and through flush.
- Every measured invocation represents either 10,000 or 20,000 logs and reports the complete one-minute batch cost.
- Reports include time per operation, ratio, managed allocation per operation, and GC counts.
- Benchmark setup and the post-iteration `BufferOnly` flush do not contribute to measured CPU or allocation results.
