# Project Rules

Conventions, preferences, and domain knowledge.

## Product Context

`yoavarad/npoi` is a permanent fork of [nissl-lab/npoi](https://github.com/nissl-lab/npoi) (a C# port of Apache POI).

- **Current goal:** serve [ole-extractor](https://github.com/yoavarad/ole-extractor), which consumes this repo as the submodule `third_party/npoi` and references only `main/NPOI.Core.csproj` (HSSF `.xls` bodies, HPSF/POIFS legacy metadata).
- **Later goal:** improve this repo broadly. Changes are not constrained by upstream acceptance.

## Known Gotchas

- **ole-extractor may only pin commits reachable from `master` or a tag**, never a feature branch -- a deleted branch breaks every fresh clone of ole-extractor.
- **Building needs the .NET 10 SDK** (`global.json` pins 10.0.100, `rollForward: latestFeature`). `Directory.Build.props` multi-targets `net472;netstandard2.0;netstandard2.1;net8.0;net10.0` and sets `TreatWarningsAsErrors`; with only SDK 8/9 installed, builds fail with `NETSDK1045`. The Desktop Runtime alone is not enough -- install the SDK.
- **Timings on SDK 10.0.401 (this machine, 2026-09-28):** cold `dotnet build solution/NPOI.Core.Test.sln` ~1m40s; `dotnet test --no-build` of the whole solution (net472 + net10.0) ~2m; per project on net10.0: NPOI.TestCases ~41s, OOXML.TestCases ~1m, OpenXml4Net.TestCases ~3s. Size verification plugin timeouts from these.
- **`Microsoft.SourceLink.GitHub` must be >= 10.0.303** (pinned 10.0.401 in `Directory.Packages.props`). 8.0.0 drags in `Microsoft.Build.Tasks.Git` 8.0.0, which has advisory GHSA-23fw-v26w-5fgq (CVE-2026-62900, no 8.x fix); with `TreatWarningsAsErrors` the NU1902 audit warning fails restore for every project.
- **One OOXML test is flaky when both TFMs run in parallel** (`dotnet test` of the whole solution failed 1 test on net472 and net10.0; rerunning net10.0 alone passed 1864/1864). Not yet identified -- suspect shared temp files.
- **The stock ydk `dotnet-*` verification plugins pick the wrong target here.** They build/test/format the alphabetically-first `.sln`, which is `benchmarks/NPOI.Benchmarks/NPOI.Benchmarks.sln`. Until project-local overrides land in `.ydk/verifications/`, don't trust their result on `.cs` changes. The real solutions are `solution/NPOI.Core.sln` (libraries) and `solution/NPOI.Core.Test.sln` (libraries + tests).
