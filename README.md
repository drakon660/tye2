# Session Summary

## 2026-03-15

Completed work (March 14–15):

### .NET 10 upgrade
- Upgraded `Directory.Build.props` from `net8.0`/LangVersion 12.0 to `net10.0`/LangVersion 14.0 (src projects only, not test projects).
- Upgraded `Microsoft.Extensions.Logging` to 10.0.0/10.0.5 across Tye2.Core and Tye2.Hosting.Diagnostics.
- Upgraded `Microsoft.Extensions.Configuration` from 2.1.1 to 10.0.5 in Tye2.Extensions.Configuration.
- Upgraded `Microsoft.Extensions.FileProviders.Embedded` from 8.0.2 to 10.0.5 in Tye2.Hosting.
- Upgraded `Refit.HttpClientFactory` from 7.0.0 to 10.0.1 in E2E tests.

### Package updates
- Upgraded `Serilog.Sinks.Console` to 6.1.1, `Serilog.Sinks.Elasticsearch` to 10.0.0, `Serilog.Sinks.Seq` to 9.0.0.
- Upgraded `semver` to 3.0.0, `ResxSourceGenerator` to 5.0.0-1.25277.114 (later removed).
- Upgraded `Newtonsoft.Json` to 13.0.4.

### ResxSourceGenerator removal
- Removed `Microsoft.CodeAnalysis.ResxSourceGenerator` package from Tye2.Core.
- Checked in `CoreStrings.Designer.cs` as a regular source file for full IDE navigation support (Rider couldn't index source-generator-only output).

### Coverage script improvements
- Rewrote `run-coverage.ps1` to run each test method individually with per-test PASS/FAIL reporting.
- Added `-Class` filter (e.g. `-Class TyeRunTests`) to run only methods in a specific test class.
- Added `-Method` filter (e.g. `-Method NginxIngressTest`) to run a single test method.
- Changed `-E2E` flag to run only E2E tests (was additive, now exclusive).
- Added `-Unit` flag for explicit unit test selection.
- Fixed Ctrl+C handling — script now kills child `dotnet test` processes and exits cleanly.
- Fixed `ProcessStartInfo` working directory for correct project path resolution.
- Fixed parameterized test name handling (strip parameters, deduplicate, exact FQN match).

### Documentation updates
- Updated 15 markdown files replacing `Microsoft.Tye` → `tye2`, `dotnet/tye` → `drakon660/tye2` URLs.
- Fixed VitePress config social link from `vuejs/vitepress` to `drakon660/tye2`.
- Created `docs-serve.cmd` and `docs-serve.sh` scripts for VitePress dev server.

### Issue backlog
- Scanned 377 open issues from original dotnet/tye repository.
- Extracted 176 actionable issues (63 bugs, 45 enhancements, 78 feature requests) into `issues-backlog.md`.
- Generated `tye-issues.csv` with valid issue numbers.

## 2026-03-13

Completed work this week (March 8–13):

### Test coverage expansion
- Added unit tests for ConfigFactory, DockerComposeParser, ApplicationFactory, KubernetesManifestGenerator, Dockerfile generator, utility/runtime helpers, extension/output context, core branch coverage, YAML writer, ProcessUtil execute/run-async, CLI command parser, and ProcessRunner E2E scenarios.
- Switched unit tests and E2E tests to AwesomeAssertions (replacing FluentAssertions).
- Reduced unit test analyzer warnings across the board.

### xunit v3 migration
- Attempted migration of all test projects to xunit v3 (PR #68).
- Reverted the xunit v3 migration from `develop` (PR #69) after E2E issues; E2E tests continue to require xunit v2 due to `DisableTestParallelization` behavior differences and process lifecycle issues.
- Removed `WORKAROUND_SkippedDataRowTestCase` (fix shipped in xunit.runner.visualstudio 2.5.7).
- Fixed CS8620 nullable warnings in `TyeConfigurationExtensionsTest.cs`.

### Package upgrades
- Upgraded `coverlet.collector` to v8.
- Upgraded `Microsoft.NET.Test.Sdk` to 18.3.0.
- Upgraded `YamlDotNet` from 15.1.2 to 16.3.0 (fixed `EnterMapping` signature breaking change).
- Upgraded `Newtonsoft.Json` from 13.0.3 to 13.0.4.
- Upgraded `System.Reactive` from 6.0.0 to 6.1.0.
- Upgraded `Bedrock.Framework` from 0.1.62-alpha to 0.1.63-alpha.
- Upgraded `Microsoft.Diagnostics.Tracing.TraceEvent` from 3.1.9 to 3.1.30.
- Upgraded `Microsoft.Diagnostics.NETCore.Client` from 0.2.510501 to 0.2.661903.
- Upgraded `Microsoft.Extensions.Logging.ApplicationInsights` from 2.22.0 to 2.23.0.
- Upgraded `xunit.runner.visualstudio` to 3.1.5 across all test projects (backwards-compatible with xunit v2).

### Codebase cleanup
- Removed `tye2-diag-agent` project (obsolete Linux-only EventPipe sidecar).
- Removed Microsoft namespaces and set up tye2 global tool from built artifact.

### Bug fixes
- Fixed `docker-compose` port mapping patterns (additional formats supported).
- Fixed `init` command: generate `tye.yaml` from docker-compose without overwrite.
- Fixed `init` command: name compose bindings for multi-port services.

### E2E test fixes
- Removed untestable `RestartService` and `LaunchFailure` E2E tests (architectural issue: `Task.WhenAll(state.Tasks)` blocks forever in `RestartService`).
- Confirmed restart behavior is covered by `ReplicaStoppingTests` via `StoppingTokenSource.Cancel()` pattern.

### Documentation
- Added git flow branch strategy for pull requests.
- Added test coverage analysis notes.

## 2026-03-08

Completed work in this session:

- Audited the full solution for obsolete `.NET 7` targets and related references.
- Updated documentation to set `.NET 8` as the minimum supported version.
- Updated CI workflow to remove `.NET 6/.NET 7` setup and keep `.NET 8` only.
- Removed obsolete `.NET 7` E2E test assets and updated related test references.
- Migrated legacy sample/testasset target frameworks (`netcoreapp3.1`, `net5.0`, `net6.0`) to `.NET 8`.
- Stabilized Zipkin E2E scenarios (race-condition handling and startup reliability).
- Added/updated Docker cleanup tooling for Tye2 resources (containers, networks, and labels).
- Added codebase scan notes with prioritized engineering fixes (`codex-suggestions.md`, `claude-suggestion.md`).
- Added test coverage gap analysis notes (`claude-test-coverage.md`).

## 2026-03-07

Completed work in this session:

- Removed legacy `.old-ms-ci` content and related cleanup tasks from the working flow.
- Added and updated branch cleanup automation for `feature/`, `bugfix/`, `hotfix/`, and `codex/*` prefixed branches.
- Changed branch cleanup GitHub Action to run on demand (manual trigger) instead of cron schedule.
- Added release build scripts for Windows/macOS and improved local build/versioning flow.
- Added single-file build support for `tye2`.
- Implemented process restart limits to prevent infinite restart loops on fatal startup failures.
- Improved test wait logic to fail fast when replicas stop with non-zero exit codes.
- Added Docker labels for Tye2-managed containers/networks.
- Added startup cleanup for stale Docker containers/networks scoped to the current project context.
- Added E2E test teardown cleanup to remove Tye2-managed Docker resources after test runs.
