# Session Summary

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
