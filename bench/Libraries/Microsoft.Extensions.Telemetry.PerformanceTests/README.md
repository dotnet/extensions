BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1848/22H2/2022Update/SunValley2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=8.0.100-preview.7.23328.2
  [Host] : .NET 8.0.0 (8.0.23.32605), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|                         Method |          Factory |      Mean |    Error |   StdDev |    Median |   Gen0 | Allocated |
|------------------------------- |----------------- |----------:|---------:|---------:|----------:|-------:|----------:|
|               Classic_RefTypes |         Original |  78.86 ns | 0.531 ns | 0.795 ns |  78.98 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |         Original |  73.63 ns | 1.270 ns | 1.900 ns |  73.89 ns | 0.0191 |     200 B |
|   LoggerMessageDefine_RefTypes |         Original |  83.25 ns | 0.283 ns | 0.397 ns |  83.26 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |         Original |  66.04 ns | 0.334 ns | 0.489 ns |  66.04 ns | 0.0160 |     168 B |
|        ClassicCodeGen_RefTypes |         Original |  84.02 ns | 0.211 ns | 0.296 ns |  83.98 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |         Original |  65.63 ns | 0.241 ns | 0.361 ns |  65.62 ns | 0.0160 |     168 B |
|         ModernCodeGen_RefTypes |         Original |  52.71 ns | 0.835 ns | 1.171 ns |  53.36 ns |      - |         - |
|       ModernCodeGen_ValueTypes |         Original |  76.57 ns | 1.674 ns | 2.454 ns |  78.00 ns | 0.0160 |     168 B |

|               Classic_RefTypes |              New | 151.98 ns | 0.455 ns | 0.637 ns | 151.84 ns | 0.0143 |     152 B |
|             Classic_ValueTypes |              New | 140.39 ns | 2.458 ns | 3.603 ns | 139.91 ns | 0.0229 |     240 B |
|   LoggerMessageDefine_RefTypes |              New | 250.27 ns | 1.481 ns | 2.076 ns | 249.69 ns | 0.0134 |     144 B |
| LoggerMessageDefine_ValueTypes |              New | 197.08 ns | 1.029 ns | 1.476 ns | 197.08 ns | 0.0222 |     232 B |
|        ClassicCodeGen_RefTypes |              New | 254.30 ns | 1.093 ns | 1.568 ns | 254.55 ns | 0.0134 |     144 B |
|      ClassicCodeGen_ValueTypes |              New | 197.03 ns | 1.136 ns | 1.593 ns | 197.40 ns | 0.0222 |     232 B |
|         ModernCodeGen_RefTypes |              New | 117.24 ns | 1.541 ns | 2.210 ns | 116.19 ns |      - |         - |
|       ModernCodeGen_ValueTypes |              New | 125.47 ns | 0.375 ns | 0.526 ns | 125.38 ns | 0.0160 |     168 B |

|               Classic_RefTypes | NewWithEnrichers | 169.25 ns | 1.816 ns | 2.604 ns | 169.00 ns | 0.0143 |     152 B |
|             Classic_ValueTypes | NewWithEnrichers | 157.14 ns | 1.819 ns | 2.490 ns | 155.73 ns | 0.0229 |     240 B |
|   LoggerMessageDefine_RefTypes | NewWithEnrichers | 277.60 ns | 1.937 ns | 2.778 ns | 277.56 ns | 0.0134 |     144 B |
| LoggerMessageDefine_ValueTypes | NewWithEnrichers | 219.33 ns | 0.701 ns | 0.983 ns | 219.25 ns | 0.0222 |     232 B |
|        ClassicCodeGen_RefTypes | NewWithEnrichers | 280.07 ns | 1.598 ns | 2.292 ns | 278.96 ns | 0.0134 |     144 B |
|      ClassicCodeGen_ValueTypes | NewWithEnrichers | 219.81 ns | 0.396 ns | 0.555 ns | 219.70 ns | 0.0222 |     232 B |
|         ModernCodeGen_RefTypes | NewWithEnrichers | 126.74 ns | 0.947 ns | 1.296 ns | 126.90 ns |      - |         - |
|       ModernCodeGen_ValueTypes | NewWithEnrichers | 137.05 ns | 0.637 ns | 0.872 ns | 137.50 ns | 0.0160 |     168 B |