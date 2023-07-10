BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1848/22H2/2022Update/SunValley2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=8.0.100-preview.7.23328.2
  [Host] : .NET 8.0.0 (8.0.23.32605), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|                         Method |          Factory |      Mean |    Error |    StdDev |    Median |   Gen0 | Allocated |
|------------------------------- |----------------- |----------:|---------:|----------:|----------:|-------:|----------:|
|               Classic_RefTypes |         Original |  81.44 ns | 1.271 ns |  1.864 ns |  80.73 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |         Original |  93.59 ns | 0.597 ns |  0.817 ns |  93.60 ns | 0.0283 |     296 B |
|   LoggerMessageDefine_RefTypes |         Original |  93.52 ns | 0.668 ns |  0.979 ns |  93.44 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |         Original |  85.40 ns | 1.392 ns |  1.905 ns |  85.38 ns | 0.0252 |     264 B |
|        ClassicCodeGen_RefTypes |         Original |  94.38 ns | 0.495 ns |  0.677 ns |  94.34 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |         Original |  84.46 ns | 0.160 ns |  0.224 ns |  84.36 ns | 0.0252 |     264 B |
|         ModernCodeGen_RefTypes |         Original |  56.96 ns | 0.252 ns |  0.378 ns |  57.01 ns |      - |         - |
|       ModernCodeGen_ValueTypes |         Original |  77.72 ns | 0.505 ns |  0.709 ns |  77.77 ns | 0.0160 |     168 B |

|               Classic_RefTypes |              New | 166.86 ns | 5.372 ns |  8.041 ns | 166.55 ns | 0.0143 |     152 B |
|             Classic_ValueTypes |              New | 182.13 ns | 8.957 ns | 12.845 ns | 178.63 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes |              New | 181.03 ns | 4.797 ns |  7.032 ns | 178.49 ns | 0.0136 |     144 B |
| LoggerMessageDefine_ValueTypes |              New | 154.48 ns | 1.087 ns |  1.559 ns | 154.81 ns | 0.0312 |     328 B |
|        ClassicCodeGen_RefTypes |              New | 174.25 ns | 1.363 ns |  2.041 ns | 174.26 ns | 0.0136 |     144 B |
|      ClassicCodeGen_ValueTypes |              New | 149.48 ns | 0.784 ns |  1.125 ns | 149.16 ns | 0.0312 |     328 B |
|         ModernCodeGen_RefTypes |              New | 117.05 ns | 0.303 ns |  0.454 ns | 117.07 ns |      - |         - |
|       ModernCodeGen_ValueTypes |              New | 119.38 ns | 0.456 ns |  0.625 ns | 119.60 ns | 0.0160 |     168 B |

|               Classic_RefTypes | NewWithEnrichers | 176.44 ns | 0.652 ns |  0.914 ns | 176.51 ns | 0.0143 |     152 B |
|             Classic_ValueTypes | NewWithEnrichers | 183.32 ns | 2.296 ns |  3.365 ns | 181.92 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes | NewWithEnrichers | 185.09 ns | 1.298 ns |  1.861 ns | 185.16 ns | 0.0136 |     144 B |
| LoggerMessageDefine_ValueTypes | NewWithEnrichers | 169.11 ns | 0.793 ns |  1.162 ns | 168.72 ns | 0.0312 |     328 B |
|        ClassicCodeGen_RefTypes | NewWithEnrichers | 186.39 ns | 0.753 ns |  1.006 ns | 187.04 ns | 0.0136 |     144 B |
|      ClassicCodeGen_ValueTypes | NewWithEnrichers | 167.66 ns | 1.490 ns |  2.230 ns | 167.34 ns | 0.0312 |     328 B |
|         ModernCodeGen_RefTypes | NewWithEnrichers | 126.53 ns | 0.233 ns |  0.341 ns | 126.38 ns |      - |         - |
|       ModernCodeGen_ValueTypes | NewWithEnrichers | 132.49 ns | 0.688 ns |  0.987 ns | 132.15 ns | 0.0160 |     168 B |
