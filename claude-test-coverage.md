# Test Coverage Analysis

## Current State

### Test Projects
- **Tye2.UnitTests** — 861 tests across 44 test files (up from 5 files originally)
- **Tye2.E2ETests** — 38+ integration tests (11 new ProcessRunner E2E tests)
- **Tye2.Extensions.Configuration.Tests** — configuration extension tests

### Progress
Phase 1 (Critical) — **fully covered**. All 6 critical components have comprehensive unit tests.
Phase 2 (Runtime Infrastructure) — PortAssigner, ReplicaRegistry.
Phase 3 (Extensions) — Zipkin, Seq, Elastic, DiagnosticAgent, DaprExtensionConfigurationReader.
Phase 4 (Utilities) — ArgumentEscaper, NameInferer, ConfigFileFinder, OutputContext.
Phase 5 (Coverage-driven) — **complete**. SolutionFile parser, ContainerEngine detection.
Phase 7 (Testable utilities) — **complete**. ProjectReader, GitDetector, NextPortFinder, TempDirectory.
Phase 8 (CLI commands) — CLI command structure, StandardOptions, ApplicationFactoryFilter, ConfigFileFinder, InitHost, ContainerRegistry, CommandException.

---

## Covered — Core Logic (Phase 1 complete)

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| YAML Config Parsers | `YamlConfigParserTests/` (14 files) | 96 | Bindings, ingress, env vars, probes, volumes, build props, docker args, env files, extensions, registries, exceptions, edge cases |
| ConfigFactory | `ConfigFactoryTests.cs` | 35 | File routing (.yaml/.yml/.csproj/.fsproj/.sln), Docker Compose detection, FromProject, FromSolution, NormalizeServiceName |
| ApplicationFactory | `ApplicationFactoryTests.cs` | 44 | Image/external services, bindings, env vars, volumes, ingress, extensions, filters, registry, probes, dependencies |
| DockerComposeParser | `DockerComposeParserTests.cs` | 45 | Service parsing, ports, env (sequence + mapping), build section, top-level keys, ignored service keys, error handling |
| KubernetesManifestGenerator | `KubernetesManifestGeneratorTests.cs` | 72 | CreateService (ports, labels, annotations, selector), CreateDeployment (images, env, probes, sidecars, volumes, secrets, pull secrets), CreateIngress (rules, paths, hosts, API version) |
| DockerfileGenerator | `DockerfileGeneratorTests.cs` | 60 | TagIs50OrNewer, ApplyContainerDefaults (base/build/image names+tags, registry, TFM routing, .NET Core vs .NET 5+ paths), WriteDockerfile (multiphase, local publish, args/CMD, UTF-8 encoding) |

## Covered — Runtime Infrastructure (Phase 2 partial)

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| PortAssigner | `PortAssignerTests.cs` | 15 | Skip without RunInfo, auto-assign ports, preserve existing, replica mapping, readiness proxy, HTTP/HTTPS container port defaults, multiple bindings/services, edge cases |
| ReplicaRegistry | `ReplicaRegistryTests.cs` | 17 | Write/read/delete events, JSON serialization, separate stores, dispose cleanup, roundtrip, concurrent writes |
| ProcessRunner | `ProcessRunnerTests.cs` | 36 | ProcessRunnerOptions (defaults, constants, ShouldDebugService, ShouldWatchService, FromHostOptions), ProcessRunner constructor, KillProcessAsync, Service.State (Unknown/Started/Starting/Stopped/Failed/Degraded), ServiceType, CachedLogs, Restarts |

## Covered — Extensions (Phase 3 partial)

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| Zipkin extension | `ZipkinExtensionTests.cs` | 11 | Service injection (container, image, binding, port), dependency wiring, duplicate detection, trace provider config, deploy sidecar |
| Seq extension | `SeqExtensionTests.cs` | 12 | Service injection (container, image, binding, env var), logPath volume, dependency wiring, duplicate detection, logging provider, deploy sidecar |
| Elastic extension | `ElasticStackExtensionTests.cs` | 14 | Service injection (container, image, kibana+http bindings), logPath volume, dependency wiring, duplicate detection, logging provider, deploy sidecar + kibana binding removal |
| DiagnosticAgent | `DiagnosticAgentTests.cs` | 10 | GetOrAddSidecar (name, image, args, relocate diagnostics), idempotent add, existing sidecar reuse |
| DaprExtensionConfigurationReader | `DaprExtensionConfigurationReaderTests.cs` | 18 | Empty config defaults, all common properties, string→int/bool coercion, invalid/null values, services parsing (single/multi/case-insensitive), service common props, non-dict skip, global+service config, enabled false, bool false as string, unknown keys, real-world configs |

## Covered — Utilities (Phase 4 partial)

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| OutputContext | `OutputContextTests.cs` | 19 | WriteInfoLine/WriteDebugLine/WriteAlwaysLine verbosity filtering, WriteCommandLine, BeginStep/EndStep indentation, nested steps, StepTracker lifecycle, CapturedCommandOutput stdout/stderr, constructor validation |
| ArgumentEscaper | `ArgumentEscaperTests.cs` | 17 | Simple args, whitespace quoting, backslash escaping, embedded quotes, already-quoted strings, mixed args, real-world examples |
| NameInferer | `NameInfererTests.cs` | 9 | .sln/.csproj/.fsproj name extraction, YAML directory fallback, case normalization, null input |
| ConfigFileFinder | `ConfigFileFinderTests.cs` | 14 | tye.yaml/yml, docker-compose, .csproj/.fsproj/.sln, priority order, empty dir, multiple matches, custom formats |
| SolutionFile parser | `SolutionFileTests.cs` | 28 | Single/multi/mixed project parsing, project types (C#/F#/folder), GUIDs, relative/absolute paths, configurations, nested projects, solution folders, solution filters (.slnf), IsBuildableProject, version detection, error handling |
| ContainerEngine | `ContainerEngineTests.cs` | 15 | Auto-detect, explicit Docker/Podman, IsUsable, ContainerHost, AspNetUrlsHost, IsPodman, CommandName throw on unusable, s_default singleton override, ContainerEngineType enum |
| ProjectReader | `ProjectReaderTests.cs` | 26 | EnumerateProjects for .csproj/.fsproj and solution-folder filtering; metadata parsing for version/TFM/frameworks/base image; path normalization; Azure Functions metadata parsing and null-guard exceptions |
| GitDetector | `GitDetectorTests.cs` | 5 | Singleton instance, IsGitInstalled lazy detection, repeated call consistency |
| NextPortFinder | `NextPortFinderTests.cs` | 6 | Port range validation, uniqueness, availability, ephemeral range |
| TempDirectory | `TempDirectoryTests.cs` | 10 | Create, directory exists, absolute path, dispose cleanup (files, subdirs), constructor, temp path location |

## Covered — Pre-existing Tests

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| YAML Deserialization | `TyeDeserializationTests.cs` | 60 | Pre-existing deserialization tests |
| YAML Validation | `TyeDeserializationValidationTests.cs` | 18 | Pre-existing validation tests |
| Service Model | `ServiceUnitTests.cs` | 13 | Pre-existing service model tests |
| DefaultOptionsMiddleware | `DefaultOptionsMiddlewareTests.cs` | 6 | Pre-existing middleware tests |
| Ansi2HtmlConverter | `Ansi2HtmlConverterTests.cs` | 2 | Pre-existing converter tests |

---

## Coverage Analysis (from cobertura XML, 2026-03-11)

### Well Covered (>80%) — No action needed
Most core logic, serialization, config model, and hosting model classes are well covered by the combination of unit and E2E tests.

### Partially Covered (1-50%) — Best candidates for improvement

| File | Line Coverage | Recommendation |
|------|-------------|----------------|
| TyeDashboardApi.cs | ~45% | **Done** — 11 E2E tests covering all endpoints |
| DaprExtensionConfigurationReader.cs | ~47% | **Done** — 18 unit tests added |
| ContainerEngine*.cs / DockerDetector.cs | 32-49% | **Done** — 15 unit tests added |
| DockerComposeParser.cs | ~37% | E2E: `tye run` with docker-compose.yml test asset |
| ValidateSecretStep.cs | ~36% | E2E: service with secret bindings + deploy validation |
| SolutionFile.cs | 34-42% | **Done** — 28 unit tests added |
| Application.cs | ~11.5% | **Partial** — 2 E2E tests (bindings, replicas, env vars, single-project) |

### Uncovered (0%) — 240 files
Many are test infrastructure, generated code, or obj/ artifacts. Key uncovered production files:
- `src/Tye2.Hosting/Watch/` (11 files) — file system watchers
- `src/Tye2.Hosting.Diagnostics/` (5 files) — diagnostic collectors
- ~~CLI command handlers (`Program.*.cs`)~~ **Done** — 62 tests in `CliCommandTests.cs`

## Next Steps Plan

### Phase 5 — Unit Tests (no external deps)
- [x] **SolutionFile parser** — 28 tests: parse .sln content, project types, nested projects, solution filters ✓
- [x] **ContainerEngine detection** — 15 tests: Docker/Podman detection, IsUsable, host config, singleton ✓

### Phase 6 — E2E Tests (require Docker)
- [x] **Dashboard API test** — 11 tests: ServiceIndex, ApplicationIndex, Services list, Service by name, 404s, Logs, Metrics (text+JSON), MetricsByName ✓
- [x] **Multi-service bindings test** — 2 tests: custom bindings, replicas, env vars, single-project run ✓
- [ ] **Docker Compose test** — `tye run` with docker-compose.yml (requires Docker images, deferred)
- [ ] **Secret validation test** — requires Kubernetes cluster (deferred)

### Phase 7 — Remaining testable utilities
- [x] **ProjectReader** — 26 tests covering project enumeration + metadata parsing (version/TFM/frameworks/Azure Functions) ✓
- [x] **GitDetector** — 5 tests: singleton, lazy detection, consistency ✓
- [x] **NextPortFinder** — 6 tests: range, uniqueness, availability ✓
- [x] **TempDirectory** — 10 tests: create, dispose cleanup, paths ✓

### Phase 8 — Hard-to-test components (if needed)
- [ ] **Dapr extension** — largest extension, process dependency
- [ ] **File watching** — filesystem watchers, MSBuild integration
- [ ] **Diagnostics** — diagnostic collectors
- [x] **CLI commands** — 62 tests: command structure (7 commands), StandardOptions (13 options), ApplicationFactoryFilter, ConfigFileFinder, InitHost, ContainerRegistry, CommandException, command parsing ✓

---

## Remaining Gaps

### High Priority — Runtime Infrastructure (untested)

| Component | File(s) | Lines | Testability |
|-----------|---------|-------|-------------|
| ProcessRunner | `src/Tye2.Hosting/ProcessRunner.cs` | ~300 | **Covered** — 36 unit tests (options, service model, state) + 11 E2E tests (process start, PID, HTTP, replicas, restart, args, env vars, logs, bindings, no-build, multi-service) |
| DockerRunner | `src/Tye2.Hosting/DockerRunner.cs` | ~300 | Hard — Docker CLI interaction, container lifecycle |
| ReplicaMonitor | `src/Tye2.Hosting/ReplicaMonitor.cs` | ~200 | Hard — tightly coupled to HTTP probing, timers, Rx subscriptions |
| ProxyService | `src/Tye2.Hosting/ProxyService.cs` | ~300 | Hard — socket proxy, connection forwarding |
| HttpProxyService | `src/Tye2.Hosting/HttpProxyService.cs` | ~300 | Hard — HTTP proxy, load balancing |
| TyeHost | `src/Tye2.Hosting/TyeHost.cs` | ~200 | Hard — orchestrates startup/shutdown of all services |

### Medium Priority — Extensions (partially tested)

| Component | File(s) | Status |
|-----------|---------|--------|
| Dapr extension | `src/Tye2.Extensions/Dapr/DaprExtension.cs` | Untested — largest extension (~390 lines), has process dependency (VerifyDaprInitialized) |
| File watching | `src/Tye2.Hosting/Watch/` (11 files) | Untested — file system watchers, MSBuild integration |
| Diagnostics | `src/Tye2.Hosting.Diagnostics/` (5 files, ~300 lines) | Untested |

### Lower Priority — Utilities (untested)

- DirectoryCopy, ConsoleExtensions, ProcessExtensions


