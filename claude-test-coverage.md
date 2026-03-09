# Test Coverage Analysis

## Current State

### Test Projects
- **Tye2.UnitTests** — 5 test files (ANSI converter, CLI options, service state, YAML deserialization/validation)
- **Tye2.E2ETests** — 14+ integration tests (require Docker, slow)
- **Tye2.Extensions.Configuration.Tests** — configuration extension tests
- **Tye2.Test.Infrastructure** — test helpers (not tests themselves)

### Core Problem
The project relies almost entirely on E2E tests. There are ~120+ classes with zero unit tests. E2E tests cover happy paths but miss error handling, edge cases, and malformed input scenarios.

---

## Critical Gaps — Core Logic (0% unit tests)

| Component | File(s) | Lines | Status |
|-----------|---------|-------|--------|
| ApplicationFactory | `src/Tye2.Core/ApplicationFactory.cs` | ~675 | E2E only, no unit tests for core logic |
| YAML Config Parsers | `src/Tye2.Core/Serialization/` (6 files) | ~1195 | Only basic deserialization tested |
| ConfigFactory | `src/Tye2.Core/ConfigModel/ConfigFactory.cs` | ~118 | Zero tests |
| DockerComposeParser | `src/Tye2.Core/DockerCompose/DockerComposeParser.cs` | ~330 | Zero tests |
| Kubernetes generation | `src/Tye2.Core/KubernetesManifestGenerator.cs` + 3 files | ~500+ | Zero tests |
| DockerfileGenerator | `src/Tye2.Core/DockerfileGenerator.cs` | ~100+ | E2E only |
| Service builders | 8 builder classes | ~500+ | Zero tests |

### What's missing
- `ApplicationFactory.CreateAsync()` — path evaluation, MSBuild parsing, multi-TFM handling, cycle detection, extension loading
- Config parsers — binding parsing, ingress rules, env vars, probes, volumes, build properties, validation errors
- `ConfigFactory` — factory dispatch (YAML vs csproj vs sln), `FromProject()`, `FromSolution()`, launchSettings.json detection
- DockerComposeParser — service mapping, port/binding conversion, env var handling
- Kubernetes — ingress/service/deployment generation, labels, probes, volumes, env var injection
- DockerfileGenerator — multi-phase vs single-phase, argument escaping, image selection, entry point generation
- Service builders — initialization, dependency resolution, binding assignment, env vars, replicas

---

## High Gaps — Runtime Infrastructure (0% unit tests)

| Component | File(s) | Lines | Status |
|-----------|---------|-------|--------|
| ProcessRunner | `src/Tye2.Hosting/ProcessRunner.cs` | ~300 | Zero tests |
| DockerRunner | `src/Tye2.Hosting/DockerRunner.cs` | ~300 | Zero tests |
| PortAssigner | `src/Tye2.Hosting/PortAssigner.cs` | ~150 | Zero tests |
| ReplicaMonitor | `src/Tye2.Hosting/ReplicaMonitor.cs` | ~200 | Zero tests |
| ProxyService | `src/Tye2.Hosting/ProxyService.cs` | ~300 | Zero tests |
| HttpProxyService | `src/Tye2.Hosting/HttpProxyService.cs` | ~300 | Zero tests |
| File watching | `src/Tye2.Hosting/Watch/` (4 files) | ~600 | Zero tests |
| Diagnostics | `src/Tye2.Hosting.Diagnostics/` (5 files) | ~300 | Zero tests |
| TyeHost | `src/Tye2.Hosting/TyeHost.cs` | ~200 | Zero tests |
| ReplicaRegistry | `src/Tye2.Hosting/ReplicaRegistry.cs` | ~200 | Zero tests |

### What's missing
- Process spawning, argument/env passing, output streams, signal forwarding, exit codes
- Container creation, port mapping, volume mounting, status tracking, cleanup
- Port assignment, conflict detection, replica port mapping
- Health/readiness/liveness probes, timeout handling, replica state transitions
- Socket proxy, HTTP proxy, load balancing, connection forwarding, route matching
- File system events, debouncing, build triggers, exclusion patterns
- Event pipe sessions, log/metric/trace collection, provider configuration
- Application startup/shutdown orchestration, service ordering, dependency waiting

---

## Medium Gaps — Extensions & Utilities (0% tests)

### Extensions
| Extension | File | Status |
|-----------|------|--------|
| Dapr | `src/Tye2.Extensions/Dapr/DaprExtension.cs` | Zero tests |
| Zipkin | `src/Tye2.Extensions/Zipkin/ZipkinExtension.cs` | Zero tests |
| Seq | `src/Tye2.Extensions/Seq/SeqExtensions.cs` | Zero tests |
| Elastic | `src/Tye2.Extensions/Elastic/ElasticStackExtension.cs` | Zero tests |
| Extension framework | `src/Tye2.Core/Extension.cs`, `ExtensionContext.cs` | Zero tests |

### Utilities (all untested)
- NextPortFinder, ArgumentEscaper, ConfigFileFinder, NameInferer
- ContainerEngine, ProjectReader, GitDetector
- ConsoleExtensions, ProcessExtensions, DirectoryCopy, TempDirectory

### CLI Commands
- `Program.*.cs` files (~500+ lines) — E2E only, no unit tests for argument parsing, validation, error handling

---

## Recommended Test Priority

### Phase 1 — Critical
1. **YAML parsers + ConfigFactory** — handle user input, most likely to hit edge cases
2. **ApplicationFactory** — central orchestrator, complex branching logic
3. **Kubernetes generation** — produces external artifacts, hard to debug without tests
4. **DockerComposeParser** — user-facing, crash-prone (Split without bounds checks)

### Phase 2 — High
5. **ProcessRunner + DockerRunner** — runtime correctness
6. **ReplicaMonitor** — state machine logic, health checks
7. **PortAssigner** — port conflicts cause hard-to-debug failures
8. **ProxyService + HttpProxyService** — networking edge cases

### Phase 3 — Medium
9. **Extension framework + implementations**
10. **File watching system**
11. **Diagnostics collection**
12. **Utility classes**
