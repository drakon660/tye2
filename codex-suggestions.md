# Codex Suggestions

## Priority Fixes

1. Security vulnerability in core dependency
- File: `src/Tye2.Core/Tye2.Core.csproj`
- Package: `KubernetesClient 6.0.26` (`NU1902` moderate vulnerability)
- Why: Direct security risk in a core runtime dependency.

2. Very old or preview infra packages in runtime path
- File: `src/Tye2.Core/Tye2.Core.csproj`
  - `System.CommandLine` beta-era versions
- File: `src/Tye2.Hosting.Diagnostics/Tye2.Hosting.Diagnostics.csproj`
  - `OpenTelemetry 0.2.0-alpha.*`
- File: `src/Tye2.Hosting/Tye2.Hosting.csproj`
  - `Bedrock.Framework 0.1.62-alpha...`
- Why: Stability and compatibility risks over time.

3. Legacy target frameworks still present (mostly samples/test assets)
- Example files:
  - `samples/azure-functions/frontend-backend/backend/backend.csproj` (`netcoreapp3.1`)
  - `samples/voting/vote/vote.csproj` (`net5.0`)
  - `samples/frontend-backend/backend/backend.csproj` (`net6.0`)
  - `test/Tye2.E2ETests/testassets/projects/azure-functions/backend/backend.csproj` (`net6.0`)
- Why: Obsolete runtime targets cause local/environment friction and reduce maintenance clarity.

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

## Suggested Execution Order

1. Upgrade/patch `KubernetesClient`.
2. Decide policy for legacy samples/test assets: upgrade vs archive/mark unsupported.
3. Stabilize Zipkin E2E test null-safety and retry handling.
4. Update old preview/beta package set with compatibility checks.
5. Burn down TODO hotspots in hosting/core pipeline.
