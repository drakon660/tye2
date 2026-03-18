# Codex Suggestions

## Priority Fixes

1. Security vulnerability in core dependency
- File: `src/Tye2.Core/Tye2.Core.csproj`
- Package: `KubernetesClient 6.0.26` (`NU1902` moderate vulnerability)
- Why: Direct security risk in a core runtime dependency.

2. Very old or preview infra packages in runtime path
- File: `src/Tye2.Core/Tye2.Core.csproj`
  - `System.CommandLine 2.0.0-beta1.20071.2` — beta-era, no stable release exists
- File: `src/Tye2.Hosting.Diagnostics/Tye2.Hosting.Diagnostics.csproj`
  - `OpenTelemetry 0.2.0-alpha.179` — extremely old alpha, current stable is 1.x
  - `OpenTelemetry.Exporter.Zipkin 0.2.0-alpha.179` — same
- Why: Stability and compatibility risks. OpenTelemetry upgrade is a significant effort (API completely changed since 0.2).

3. Samples/test assets still on .NET 8
- All samples and test assets target `net8.0` with explicit `<TargetFramework>` overrides.
- Src projects are on `net10.0` via `Directory.Build.props`.
- Test projects (Tye2.UnitTests, E2E, etc.) inherit `net10.0` from Directory.Build.props.
- Why: Samples need to stay on net8.0 for now since Tye orchestrates building them, and they need to match what users would actually run.

4. Zipkin E2E test hardening still needed
- File: `test/Tye2.E2ETests/TyeRunTests.cs` (`RunFrontendBackendProjectWithZipkin`)
- Gaps:
  - Non-null-safe access to `x.LocalEndpoint.ServiceName` in some predicates
  - Retry catches only `HttpRequestException` (other transient Refit/API errors can still fail early)
- Why: Reduces flaky behavior across Debug/Release and different machines.

5. Technical-debt hotspots to schedule
- File: `src/Tye2.Hosting/DockerRunner.cs` (TODO around .NET 8 check)
- File: `src/Tye2.Core/ApplicationFactory.cs` (TODOs around liveness/null handling)
- File: `src/Tye2.Hosting/ProcessRunner.cs` (build/debug TODOs)
- Why: These are in failure-path/high-traffic orchestration code and worth cleanup.

## Completed (as of 2026-03-15)

- [x] Upgraded Serilog sinks (Console 6.1.1, Elasticsearch 10.0.0, Seq 9.0.0)
- [x] Upgraded semver to 3.0.0, Newtonsoft.Json to 13.0.4
- [x] Upgraded System.Reactive to 6.1.0, Bedrock.Framework to 0.1.63-alpha
- [x] Upgraded TraceEvent to 3.1.30, NETCore.Client to 0.2.661903, ApplicationInsights to 2.23.0
- [x] Upgraded xunit.runner.visualstudio to 3.1.5 across all test projects
- [x] Upgraded Microsoft.Extensions.Logging to 10.0.0/10.0.5
- [x] Upgraded Microsoft.Extensions.Configuration from 2.1.1 to 10.0.5
- [x] Upgraded Microsoft.Extensions.FileProviders.Embedded from 8.0.2 to 10.0.5
- [x] Upgraded Refit.HttpClientFactory from 7.0.0 to 10.0.1
- [x] Removed ResxSourceGenerator; checked in CoreStrings.Designer.cs as regular source
- [x] Upgraded src projects to .NET 10 (Directory.Build.props → net10.0, LangVersion 14.0)
- [x] Removed legacy TFMs (netcoreapp3.1, net5.0, net6.0) from samples/test assets → all net8.0
- [x] Updated 15 markdown files (Microsoft.Tye → tye2, dotnet/tye → drakon660/tye2)
- [x] Scanned 377 open issues → 176 actionable items in `issues-backlog.md`

## Remaining Execution Order

1. Upgrade/patch `KubernetesClient` (security vulnerability NU1902).
2. Stabilize Zipkin E2E test null-safety and retry handling.
3. Evaluate `OpenTelemetry` upgrade path (0.2-alpha → 1.x is a major rewrite).
4. Evaluate `System.CommandLine` — no stable release exists; consider alternatives or stay on beta.
5. Burn down TODO hotspots in hosting/core pipeline (DockerRunner, ApplicationFactory, ProcessRunner).
6. Review 176 open issues in `issues-backlog.md` for quick wins.
