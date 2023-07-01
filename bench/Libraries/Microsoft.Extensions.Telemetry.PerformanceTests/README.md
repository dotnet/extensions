BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1848/22H2/2022Update/SunValley2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=8.0.100-preview.7.23328.2
  [Host] : .NET 8.0.0 (8.0.23.32605), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|                         Method |          Factory |      Mean |    Error |   StdDev |    Median |   Gen0 | Allocated |
|------------------------------- |----------------- |----------:|---------:|---------:|----------:|-------:|----------:|
|               Classic_RefTypes |         Original |  79.32 ns | 0.274 ns | 0.375 ns |  79.41 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |         Original |  72.76 ns | 0.820 ns | 1.150 ns |  73.48 ns | 0.0191 |     200 B |
|   LoggerMessageDefine_RefTypes |         Original |  83.42 ns | 0.287 ns | 0.412 ns |  83.41 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |         Original |  66.94 ns | 0.257 ns | 0.368 ns |  66.85 ns | 0.0160 |     168 B |
|        ClassicCodeGen_RefTypes |         Original |  83.47 ns | 0.551 ns | 0.825 ns |  83.13 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |         Original |  68.47 ns | 1.345 ns | 2.013 ns |  67.71 ns | 0.0160 |     168 B |
|         ModernCodeGen_RefTypes |         Original |  47.81 ns | 0.542 ns | 0.794 ns |  47.51 ns |      - |         - |
|       ModernCodeGen_ValueTypes |         Original |  70.35 ns | 0.420 ns | 0.589 ns |  70.25 ns | 0.0160 |     168 B |

|               Classic_RefTypes |              New | 152.75 ns | 1.037 ns | 1.487 ns | 152.89 ns | 0.0143 |     152 B |
|             Classic_ValueTypes |              New | 142.16 ns | 0.796 ns | 1.166 ns | 142.11 ns | 0.0229 |     240 B |
|   LoggerMessageDefine_RefTypes |              New | 258.15 ns | 1.430 ns | 2.004 ns | 257.90 ns | 0.0134 |     144 B |
| LoggerMessageDefine_ValueTypes |              New | 197.78 ns | 1.153 ns | 1.726 ns | 197.54 ns | 0.0222 |     232 B |
|        ClassicCodeGen_RefTypes |              New | 261.60 ns | 0.730 ns | 1.047 ns | 261.58 ns | 0.0134 |     144 B |
|      ClassicCodeGen_ValueTypes |              New | 200.00 ns | 0.478 ns | 0.716 ns | 199.85 ns | 0.0222 |     232 B |
|         ModernCodeGen_RefTypes |              New | 103.86 ns | 0.327 ns | 0.470 ns | 103.79 ns |      - |         - |
|       ModernCodeGen_ValueTypes |              New | 115.91 ns | 1.019 ns | 1.525 ns | 115.48 ns | 0.0160 |     168 B |

|               Classic_RefTypes | NewWithEnrichers | 176.60 ns | 1.399 ns | 2.093 ns | 176.44 ns | 0.0143 |     152 B |
|             Classic_ValueTypes | NewWithEnrichers | 161.15 ns | 0.758 ns | 1.111 ns | 161.16 ns | 0.0229 |     240 B |
|   LoggerMessageDefine_RefTypes | NewWithEnrichers | 305.36 ns | 1.035 ns | 1.549 ns | 305.52 ns | 0.0134 |     144 B |
| LoggerMessageDefine_ValueTypes | NewWithEnrichers | 224.84 ns | 1.096 ns | 1.641 ns | 224.82 ns | 0.0222 |     232 B |
|        ClassicCodeGen_RefTypes | NewWithEnrichers | 304.66 ns | 0.651 ns | 0.954 ns | 304.48 ns | 0.0134 |     144 B |
|      ClassicCodeGen_ValueTypes | NewWithEnrichers | 226.04 ns | 1.015 ns | 1.488 ns | 225.82 ns | 0.0222 |     232 B |
|         ModernCodeGen_RefTypes | NewWithEnrichers | 109.67 ns | 0.427 ns | 0.612 ns | 109.56 ns |      - |         - |
|       ModernCodeGen_ValueTypes | NewWithEnrichers | 123.62 ns | 0.673 ns | 0.986 ns | 123.40 ns | 0.0160 |     168 B |
