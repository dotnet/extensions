BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1848/22H2/2022Update/SunValley2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=8.0.100-preview.7.23328.2
  [Host] : .NET 8.0.0 (8.0.23.32605), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|                         Method |          Factory |      Mean |    Error |    StdDev |    Median |   Gen0 | Allocated |
|------------------------------- |----------------- |----------:|---------:|----------:|----------:|-------:|----------:|
|               Classic_RefTypes |         Original |  84.99 ns | 1.099 ns |  1.540 ns |  84.26 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |         Original | 100.60 ns | 1.673 ns |  2.345 ns | 100.74 ns | 0.0283 |     296 B |
|   LoggerMessageDefine_RefTypes |         Original |  89.03 ns | 0.878 ns |  1.202 ns |  89.17 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |         Original |  86.92 ns | 0.376 ns |  0.540 ns |  87.03 ns | 0.0252 |     264 B |
|        ClassicCodeGen_RefTypes |         Original |  89.00 ns | 0.604 ns |  0.806 ns |  89.21 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |         Original |  85.99 ns | 1.084 ns |  1.554 ns |  85.89 ns | 0.0252 |     264 B |
|         ModernCodeGen_RefTypes |         Original |  48.02 ns | 0.229 ns |  0.328 ns |  48.05 ns |      - |         - |
|       ModernCodeGen_ValueTypes |         Original |  75.90 ns | 1.843 ns |  2.701 ns |  75.46 ns | 0.0160 |     168 B |

|               Classic_RefTypes |              New | 176.58 ns | 5.359 ns |  8.021 ns | 172.94 ns | 0.0143 |     152 B |
|             Classic_ValueTypes |              New | 161.10 ns | 0.361 ns |  0.506 ns | 161.04 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes |              New | 255.94 ns | 1.165 ns |  1.744 ns | 256.06 ns | 0.0134 |     144 B |
| LoggerMessageDefine_ValueTypes |              New | 229.43 ns | 6.901 ns | 10.329 ns | 224.17 ns | 0.0312 |     328 B |
|        ClassicCodeGen_RefTypes |              New | 266.09 ns | 0.764 ns |  1.046 ns | 266.22 ns | 0.0134 |     144 B |
|      ClassicCodeGen_ValueTypes |              New | 218.44 ns | 0.878 ns |  1.259 ns | 219.02 ns | 0.0312 |     328 B |
|         ModernCodeGen_RefTypes |              New |  53.74 ns | 0.628 ns |  0.881 ns |  54.20 ns |      - |         - |
|       ModernCodeGen_ValueTypes |              New |  77.29 ns | 0.160 ns |  0.224 ns |  77.28 ns | 0.0160 |     168 B |

|               Classic_RefTypes | NewWithEnrichers | 184.00 ns | 0.518 ns |  0.727 ns | 183.91 ns | 0.0143 |     152 B |
|             Classic_ValueTypes | NewWithEnrichers | 181.24 ns | 0.460 ns |  0.645 ns | 181.10 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes | NewWithEnrichers | 284.23 ns | 1.120 ns |  1.495 ns | 284.60 ns | 0.0134 |     144 B |
| LoggerMessageDefine_ValueTypes | NewWithEnrichers | 243.61 ns | 1.494 ns |  2.044 ns | 243.56 ns | 0.0310 |     328 B |
|        ClassicCodeGen_RefTypes | NewWithEnrichers | 286.73 ns | 1.140 ns |  1.635 ns | 286.97 ns | 0.0134 |     144 B |
|      ClassicCodeGen_ValueTypes | NewWithEnrichers | 240.88 ns | 2.434 ns |  3.491 ns | 240.16 ns | 0.0310 |     328 B |
|         ModernCodeGen_RefTypes | NewWithEnrichers |  57.33 ns | 0.286 ns |  0.428 ns |  57.28 ns |      - |         - |
|       ModernCodeGen_ValueTypes | NewWithEnrichers |  80.39 ns | 0.630 ns |  0.923 ns |  80.89 ns | 0.0160 |     168 B |
