.NET SDK=8.0.100-preview.7.23328.2
  [Host] : .NET 8.0.0 (8.0.23.32605), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10

|                         Method |          Factory |     Mean |    Error |   StdDev |   Median |   Gen0 | Allocated |
|------------------------------- |----------------- |---------:|---------:|---------:|---------:|-------:|----------:|
|               Classic_RefTypes |         Original | 76.06 ns | 1.577 ns | 2.262 ns | 75.80 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |         Original | 75.92 ns | 0.487 ns | 0.699 ns | 75.68 ns | 0.0191 |     200 B |
|   LoggerMessageDefine_RefTypes |         Original | 78.57 ns | 0.345 ns | 0.495 ns | 78.51 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |         Original | 67.55 ns | 0.765 ns | 1.097 ns | 67.39 ns | 0.0160 |     168 B |
|        ClassicCodeGen_RefTypes |         Original | 82.63 ns | 0.272 ns | 0.407 ns | 82.64 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |         Original | 67.44 ns | 0.671 ns | 0.983 ns | 67.23 ns | 0.0160 |     168 B |
|         ModernCodeGen_RefTypes |         Original | 51.74 ns | 0.226 ns | 0.310 ns | 51.59 ns |      - |         - |
|       ModernCodeGen_ValueTypes |         Original | 72.06 ns | 0.695 ns | 1.018 ns | 72.08 ns | 0.0160 |     168 B |
|               Classic_RefTypes |              New | 75.18 ns | 0.465 ns | 0.682 ns | 74.96 ns | 0.0106 |     112 B |
|             Classic_ValueTypes |              New | 72.24 ns | 0.684 ns | 1.023 ns | 71.77 ns | 0.0191 |     200 B |
|   LoggerMessageDefine_RefTypes |              New | 79.04 ns | 0.522 ns | 0.765 ns | 78.80 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes |              New | 66.15 ns | 1.029 ns | 1.475 ns | 66.22 ns | 0.0160 |     168 B |
|        ClassicCodeGen_RefTypes |              New | 84.10 ns | 1.354 ns | 1.985 ns | 83.27 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes |              New | 67.17 ns | 1.115 ns | 1.526 ns | 68.28 ns | 0.0160 |     168 B |
|         ModernCodeGen_RefTypes |              New | 51.13 ns | 0.502 ns | 0.752 ns | 50.86 ns |      - |         - |
|       ModernCodeGen_ValueTypes |              New | 71.11 ns | 0.553 ns | 0.757 ns | 70.97 ns | 0.0160 |     168 B |
|               Classic_RefTypes | NewWithEnrichers | 77.00 ns | 1.636 ns | 2.397 ns | 76.55 ns | 0.0106 |     112 B |
|             Classic_ValueTypes | NewWithEnrichers | 73.37 ns | 1.133 ns | 1.474 ns | 73.33 ns | 0.0191 |     200 B |
|   LoggerMessageDefine_RefTypes | NewWithEnrichers | 78.83 ns | 0.483 ns | 0.693 ns | 78.85 ns | 0.0068 |      72 B |
| LoggerMessageDefine_ValueTypes | NewWithEnrichers | 67.83 ns | 0.229 ns | 0.336 ns | 67.83 ns | 0.0160 |     168 B |
|        ClassicCodeGen_RefTypes | NewWithEnrichers | 83.21 ns | 0.395 ns | 0.578 ns | 82.99 ns | 0.0068 |      72 B |
|      ClassicCodeGen_ValueTypes | NewWithEnrichers | 68.00 ns | 0.640 ns | 0.958 ns | 67.98 ns | 0.0160 |     168 B |
|         ModernCodeGen_RefTypes | NewWithEnrichers | 53.94 ns | 1.883 ns | 2.640 ns | 56.12 ns |      - |         - |
|       ModernCodeGen_ValueTypes | NewWithEnrichers | 73.72 ns | 1.588 ns | 2.278 ns | 73.09 ns | 0.0160 |     168 B |
