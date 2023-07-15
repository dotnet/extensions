BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1848/22H2/2022Update/SunValley2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=8.0.100-preview.7.23328.2
  [Host] : .NET 8.0.0 (8.0.23.32605), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|                         Method |          Factory |      Mean |    Error |   StdDev |    Median |   Gen0 | Allocated |
|------------------------------- |----------------- |----------:|---------:|---------:|----------:|-------:|----------:|
|               Classic_RefTypes |         Original |  88.27 ns | 1.337 ns | 1.959 ns |  87.65 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |         Original |  89.72 ns | 0.699 ns | 1.024 ns |  89.72 ns | 0.0283 |     296 B |
|   LoggerMessageDefine_RefTypes |         Original |  86.65 ns | 2.792 ns | 4.179 ns |  84.97 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |         Original |  80.61 ns | 0.762 ns | 1.092 ns |  80.58 ns | 0.0252 |     264 B |
|        ClassicCodeGen_RefTypes |         Original |  80.05 ns | 0.589 ns | 0.863 ns |  79.93 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |         Original |  79.52 ns | 0.665 ns | 0.932 ns |  79.69 ns | 0.0252 |     264 B |
|         ModernCodeGen_RefTypes |         Original |  59.34 ns | 0.508 ns | 0.745 ns |  59.02 ns |      - |         - |
|       ModernCodeGen_ValueTypes |         Original |  77.00 ns | 1.429 ns | 2.139 ns |  77.20 ns | 0.0160 |     168 B |

|               Classic_RefTypes |              New | 139.09 ns | 0.966 ns | 1.446 ns | 138.47 ns | 0.0143 |     152 B |
|             Classic_ValueTypes |              New | 143.90 ns | 1.165 ns | 1.708 ns | 143.31 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes |              New | 145.32 ns | 1.270 ns | 1.862 ns | 146.37 ns | 0.0136 |     144 B |
| LoggerMessageDefine_ValueTypes |              New | 133.70 ns | 0.575 ns | 0.860 ns | 133.69 ns | 0.0312 |     328 B |
|        ClassicCodeGen_RefTypes |              New | 144.67 ns | 0.652 ns | 0.935 ns | 144.53 ns | 0.0136 |     144 B |
|      ClassicCodeGen_ValueTypes |              New | 132.62 ns | 1.427 ns | 2.092 ns | 133.32 ns | 0.0312 |     328 B |
|         ModernCodeGen_RefTypes |              New |  63.74 ns | 1.477 ns | 2.071 ns |  63.34 ns |      - |         - |
|       ModernCodeGen_ValueTypes |              New |  83.08 ns | 2.444 ns | 3.426 ns |  82.26 ns | 0.0160 |     168 B |

|               Classic_RefTypes | NewWithEnrichers | 145.13 ns | 0.491 ns | 0.705 ns | 145.02 ns | 0.0143 |     152 B |
|             Classic_ValueTypes | NewWithEnrichers | 151.56 ns | 2.216 ns | 3.248 ns | 150.42 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes | NewWithEnrichers | 156.64 ns | 2.794 ns | 3.824 ns | 155.43 ns | 0.0136 |     144 B |
| LoggerMessageDefine_ValueTypes | NewWithEnrichers | 137.69 ns | 1.132 ns | 1.623 ns | 138.27 ns | 0.0312 |     328 B |
|        ClassicCodeGen_RefTypes | NewWithEnrichers | 151.18 ns | 0.679 ns | 0.973 ns | 151.18 ns | 0.0136 |     144 B |
|      ClassicCodeGen_ValueTypes | NewWithEnrichers | 139.26 ns | 0.616 ns | 0.922 ns | 139.20 ns | 0.0312 |     328 B |
|         ModernCodeGen_RefTypes | NewWithEnrichers |  68.12 ns | 0.661 ns | 0.949 ns |  67.86 ns |      - |         - |
|       ModernCodeGen_ValueTypes | NewWithEnrichers |  87.37 ns | 1.709 ns | 2.452 ns |  86.45 ns | 0.0160 |     168 B |
