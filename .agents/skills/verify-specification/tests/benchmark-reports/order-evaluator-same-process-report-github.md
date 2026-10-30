```

BenchmarkDotNet v0.15.2, Linux Omarchy
AMD Ryzen 9 9950X3D 5.76GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 9.0.22 (9.0.2226.52218), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-OEWQXS : .NET 9.0.22 (9.0.2226.52218), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

LaunchCount=3  

```
| Method             | Categories | Mean         | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |-------------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| MergeBase_FourKeys | FourKeys   | 4,835.680 ns | 45.8314 ns | 87.1990 ns |  1.00 |    0.02 | 0.1144 |    5752 B |        1.00 |
| Branch_FourKeys    | FourKeys   | 4,700.889 ns | 33.3540 ns | 60.9897 ns |  0.97 |    0.02 | 0.0916 |    5144 B |        0.89 |
|                    |            |              |            |            |       |         |        |           |             |
| MergeBase_NoOrder  | NoOrder    |    15.002 ns |  0.2142 ns |  0.4424 ns |  1.00 |    0.04 |      - |         - |          NA |
| Branch_NoOrder     | NoOrder    |     1.145 ns |  0.0079 ns |  0.0145 ns |  0.08 |    0.00 |      - |         - |          NA |
|                    |            |              |            |            |       |         |        |           |             |
| MergeBase_OneKey   | OneKey     | 1,186.688 ns |  4.2793 ns |  8.1418 ns |  1.00 |    0.01 | 0.0305 |    1616 B |        1.00 |
| Branch_OneKey      | OneKey     | 1,186.171 ns | 14.1747 ns | 25.1955 ns |  1.00 |    0.02 | 0.0267 |    1488 B |        0.92 |
|                    |            |              |            |            |       |         |        |           |             |
| MergeBase_TwoKeys  | TwoKeys    | 2,353.985 ns | 21.4372 ns | 40.2643 ns |  1.00 |    0.02 | 0.0572 |    2936 B |        1.00 |
| Branch_TwoKeys     | TwoKeys    | 2,222.211 ns |  9.3030 ns | 16.7753 ns |  0.94 |    0.02 | 0.0458 |    2632 B |        0.90 |
