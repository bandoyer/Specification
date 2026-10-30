```

BenchmarkDotNet v0.15.2, Linux Omarchy
AMD Ryzen 9 9950X3D 5.76GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 9.0.22 (9.0.2226.52218), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 9.0.22 (9.0.2226.52218), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method      | Spec                | Mean       | Error     | StdDev    | Gen0   | Allocated |
|------------ |-------------------- |-----------:|----------:|----------:|-------:|----------:|
| **IncludeStep** | **None**                |   **8.367 ns** | **0.0441 ns** | **0.0391 ns** |      **-** |         **-** |
| **IncludeStep** | **OneString**           | **116.461 ns** | **1.9438 ns** | **1.8182 ns** | **0.0129** |     **656 B** |
| **IncludeStep** | **TwoStrings**          | **251.320 ns** | **4.9344 ns** | **7.6823 ns** | **0.0248** |    **1248 B** |
| **IncludeStep** | **StringAndExpression** | **271.156 ns** | **4.5687 ns** | **4.0500 ns** | **0.0291** |    **1480 B** |
