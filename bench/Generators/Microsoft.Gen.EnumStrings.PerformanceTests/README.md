```
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.963)
Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.100
  [Host] : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|                        Method |          Mean |         Error |        StdDev |    Gen0 | Allocated |
|------------------------------ |--------------:|--------------:|--------------:|--------:|----------:|
|                 ToStringColor |     40.991 ns |     0.5249 ns |     0.7358 ns |  0.0114 |      72 B |
|                  GetNameColor |     33.143 ns |     0.4681 ns |     0.7007 ns |       - |         - |
|        ToInvariantStringColor |      3.834 ns |     0.0178 ns |     0.0267 ns |       - |         - |
|          ToStringSmallOptions |    135.437 ns |     1.7399 ns |     2.5503 ns |  0.0470 |     296 B |
| ToInvariantStringSmallOptions |     12.207 ns |     0.0620 ns |     0.0889 ns |       - |         - |
|          ToStringLargeOptions |    193.580 ns |     2.6614 ns |     3.8169 ns |  0.0587 |     368 B |
| ToInvariantStringLargeOptions |     16.424 ns |     0.2146 ns |     0.3145 ns |       - |         - |
|                ToStringRandom | 72,026.761 ns | 1,205.1409 ns | 1,728.3770 ns | 10.8643 |   68232 B |
|       ToInvariantStringRandom | 64,554.832 ns |   581.9061 ns |   852.9504 ns |  8.0566 |   50758 B |
```
