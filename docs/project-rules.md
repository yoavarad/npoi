# Project Rules

Conventions, preferences, and domain knowledge.

## Product Context

`yoavarad/npoi` is a permanent fork of [nissl-lab/npoi](https://github.com/nissl-lab/npoi) (a C# port of Apache POI).

- **Current goal:** serve [ole-extractor](https://github.com/yoavarad/ole-extractor), which consumes this repo as the submodule `third_party/npoi` and references only `main/NPOI.Core.csproj` (HSSF `.xls` bodies, HPSF/POIFS legacy metadata).
- **Later goal:** improve this repo broadly. Changes are not constrained by upstream acceptance.

## Known Gotchas

- **ole-extractor may only pin commits reachable from `master` or a tag**, never a feature branch -- a deleted branch breaks every fresh clone of ole-extractor.
- **Building needs the .NET 10 SDK.** `Directory.Build.props` multi-targets `net472;netstandard2.0;netstandard2.1;net8.0;net10.0`, and CI installs SDK 10.0. With only SDK 8/9 installed, builds fail with `NETSDK1045`.
- **The stock ydk `dotnet-*` verification plugins pick the wrong target here.** They build/test/format the alphabetically-first `.sln`, which is `benchmarks/NPOI.Benchmarks/NPOI.Benchmarks.sln`. Until project-local overrides land in `.ydk/verifications/`, don't trust their result on `.cs` changes. The real solutions are `solution/NPOI.Core.sln` (libraries) and `solution/NPOI.Core.Test.sln` (libraries + tests).
