# Test Coverage Analysis

## Current State

### Test Projects
- **Tye2.UnitTests** — 589 tests across 36 test files (up from 5 files originally)
- **Tye2.E2ETests** — 14+ integration tests (require Docker, slow)
- **Tye2.Extensions.Configuration.Tests** — configuration extension tests

### Progress
Phase 1 (Critical) from the original analysis is now **fully covered**. All 6 critical components have comprehensive unit tests.
Phase 2 (Runtime Infrastructure) partially covered: PortAssigner, ReplicaRegistry.
Phase 3 (Extensions) partially covered: Zipkin, Seq, Elastic, DiagnosticAgent.
Phase 4 (Utilities) partially covered: ArgumentEscaper, NameInferer, ConfigFileFinder, OutputContext.

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

## Covered — Extensions (Phase 3 partial)

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| Zipkin extension | `ZipkinExtensionTests.cs` | 11 | Service injection (container, image, binding, port), dependency wiring, duplicate detection, trace provider config, deploy sidecar |
| Seq extension | `SeqExtensionTests.cs` | 12 | Service injection (container, image, binding, env var), logPath volume, dependency wiring, duplicate detection, logging provider, deploy sidecar |
| Elastic extension | `ElasticStackExtensionTests.cs` | 14 | Service injection (container, image, kibana+http bindings), logPath volume, dependency wiring, duplicate detection, logging provider, deploy sidecar + kibana binding removal |
| DiagnosticAgent | `DiagnosticAgentTests.cs` | 10 | GetOrAddSidecar (name, image, args, relocate diagnostics), idempotent add, existing sidecar reuse |

## Covered — Utilities (Phase 4 partial)

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| OutputContext | `OutputContextTests.cs` | 19 | WriteInfoLine/WriteDebugLine/WriteAlwaysLine verbosity filtering, WriteCommandLine, BeginStep/EndStep indentation, nested steps, StepTracker lifecycle, CapturedCommandOutput stdout/stderr, constructor validation |
| ArgumentEscaper | `ArgumentEscaperTests.cs` | 17 | Simple args, whitespace quoting, backslash escaping, embedded quotes, already-quoted strings, mixed args, real-world examples |
| NameInferer | `NameInfererTests.cs` | 9 | .sln/.csproj/.fsproj name extraction, YAML directory fallback, case normalization, null input |
| ConfigFileFinder | `ConfigFileFinderTests.cs` | 14 | tye.yaml/yml, docker-compose, .csproj/.fsproj/.sln, priority order, empty dir, multiple matches, custom formats |

## Covered — Pre-existing Tests

| Component | Test File(s) | Tests | Coverage |
|-----------|-------------|-------|----------|
| YAML Deserialization | `TyeDeserializationTests.cs` | 60 | Pre-existing deserialization tests |
| YAML Validation | `TyeDeserializationValidationTests.cs` | 18 | Pre-existing validation tests |
| Service Model | `ServiceUnitTests.cs` | 13 | Pre-existing service model tests |
| DefaultOptionsMiddleware | `DefaultOptionsMiddlewareTests.cs` | 6 | Pre-existing middleware tests |
| Ansi2HtmlConverter | `Ansi2HtmlConverterTests.cs` | 2 | Pre-existing converter tests |

---

## Remaining Gaps

### High Priority — Runtime Infrastructure (untested)

| Component | File(s) | Lines | Testability |
|-----------|---------|-------|-------------|
| ProcessRunner | `src/Tye2.Hosting/ProcessRunner.cs` | ~300 | Hard — spawns OS processes, signal forwarding |
| DockerRunner | `src/Tye2.Hosting/DockerRunner.cs` | ~300 | Hard — Docker CLI interaction, container lifecycle |
| ReplicaMonitor | `src/Tye2.Hosting/ReplicaMonitor.cs` | ~200 | Hard — tightly coupled to HTTP probing, timers, Rx subscriptions |
| ProxyService | `src/Tye2.Hosting/ProxyService.cs` | ~300 | Hard — socket proxy, connection forwarding |
| HttpProxyService | `src/Tye2.Hosting/HttpProxyService.cs` | ~300 | Hard — HTTP proxy, load balancing |
| TyeHost | `src/Tye2.Hosting/TyeHost.cs` | ~200 | Hard — orchestrates startup/shutdown of all services |

### Medium Priority — Extensions (partially tested)

| Component | File(s) | Status |
|-----------|---------|--------|
| Dapr extension | `src/Tye2.Extensions/Dapr/DaprExtension.cs` | Untested — largest extension (~390 lines), has process dependency (VerifyDaprInitialized) |
| DaprExtensionConfigurationReader | `src/Tye2.Extensions/Dapr/DaprExtensionConfigurationReader.cs` | Untested — pure config parsing logic, easy to test |
| File watching | `src/Tye2.Hosting/Watch/` (11 files) | Untested — file system watchers, MSBuild integration |
| Diagnostics | `src/Tye2.Hosting.Diagnostics/` (5 files, ~300 lines) | Untested |

### Lower Priority — Utilities (untested)

- NextPortFinder, ContainerEngine, ProjectReader, GitDetector
- ConsoleExtensions, ProcessExtensions, DirectoryCopy, TempDirectory
- CLI commands (`Program.*.cs` files, ~500+ lines)
