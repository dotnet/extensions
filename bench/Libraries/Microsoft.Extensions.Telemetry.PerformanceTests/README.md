BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1848/22H2/2022Update/SunValley2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=8.0.100-preview.7.23328.2
  [Host] : .NET 8.0.0 (8.0.23.32605), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|               Classic_RefTypes |         Original |  86.70 ns | 2.114 ns | 3.098 ns |  87.53 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |         Original |  95.07 ns | 1.104 ns | 1.547 ns |  96.09 ns | 0.0283 |     296 B |
|   LoggerMessageDefine_RefTypes |         Original |  80.07 ns | 1.320 ns | 1.851 ns |  80.20 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |         Original |  84.22 ns | 2.054 ns | 3.011 ns |  83.69 ns | 0.0252 |     264 B |
|        ClassicCodeGen_RefTypes |         Original |  80.36 ns | 0.517 ns | 0.690 ns |  80.39 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |         Original |  82.33 ns | 1.197 ns | 1.678 ns |  81.97 ns | 0.0252 |     264 B |
|         ModernCodeGen_RefTypes |         Original |  59.31 ns | 0.379 ns | 0.556 ns |  59.09 ns |      - |         - |
|       ModernCodeGen_ValueTypes |         Original |  78.59 ns | 1.769 ns | 2.594 ns |  78.45 ns | 0.0160 |     168 B |
|               Classic_RefTypes |              New | 138.53 ns | 0.826 ns | 1.184 ns | 138.32 ns | 0.0143 |     152 B |
|             Classic_ValueTypes |              New | 141.50 ns | 0.658 ns | 0.984 ns | 141.33 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes |              New | 149.70 ns | 0.953 ns | 1.398 ns | 149.51 ns | 0.0136 |     144 B |
| LoggerMessageDefine_ValueTypes |              New | 131.45 ns | 0.656 ns | 0.920 ns | 131.74 ns | 0.0312 |     328 B |
|        ClassicCodeGen_RefTypes |              New | 146.57 ns | 3.755 ns | 5.139 ns | 148.38 ns | 0.0136 |     144 B |
|      ClassicCodeGen_ValueTypes |              New | 129.45 ns | 0.772 ns | 1.108 ns | 129.99 ns | 0.0312 |     328 B |
|         ModernCodeGen_RefTypes |              New |  60.68 ns | 0.633 ns | 0.887 ns |  60.51 ns |      - |         - |
|       ModernCodeGen_ValueTypes |              New |  80.78 ns | 0.373 ns | 0.536 ns |  80.61 ns | 0.0160 |     168 B |
|               Classic_RefTypes | NewWithEnrichers | 144.45 ns | 0.556 ns | 0.798 ns | 144.60 ns | 0.0143 |     152 B |
|             Classic_ValueTypes | NewWithEnrichers | 148.00 ns | 0.766 ns | 1.049 ns | 147.48 ns | 0.0319 |     336 B |
|   LoggerMessageDefine_RefTypes | NewWithEnrichers | 148.39 ns | 1.016 ns | 1.457 ns | 147.84 ns | 0.0136 |     144 B |
| LoggerMessageDefine_ValueTypes | NewWithEnrichers | 137.76 ns | 0.582 ns | 0.835 ns | 137.66 ns | 0.0312 |     328 B |
|        ClassicCodeGen_RefTypes | NewWithEnrichers | 147.13 ns | 2.168 ns | 3.178 ns | 148.46 ns | 0.0136 |     144 B |
|      ClassicCodeGen_ValueTypes | NewWithEnrichers | 135.49 ns | 0.965 ns | 1.414 ns | 134.96 ns | 0.0312 |     328 B |
|         ModernCodeGen_RefTypes | NewWithEnrichers |  67.11 ns | 0.379 ns | 0.556 ns |  66.95 ns |      - |         - |
|       ModernCodeGen_ValueTypes | NewWithEnrichers |  85.60 ns | 0.710 ns | 0.972 ns |  86.24 ns | 0.0160 |     168 B |
