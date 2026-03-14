# Session Summary

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
