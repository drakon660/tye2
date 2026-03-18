# Tye2 Issues Backlog

Extracted from 377 **open** issues in the original dotnet/tye repository. Filtered to actionable bugs, enhancements, and feature requests.

---

## Bugs (63)

### Process Management

| # | Title | Description |
|---|-------|-------------|
| 633 | Tye doesn't clean up processes properly | Orphaned processes remain after exit, holding ports |
| 1088 | Crash on shutdown with many services | ObjectDisposedException for TimerAwaitable on shutdown |
| 1339 | Service keeps restarting infinitely | Service that runs normally outside tye keeps relaunching |
| 1417 | Process cleanup kills unrelated processes | Stale PIDs may belong to unrelated applications |

### Docker / Containers

| # | Title | Description |
|---|-------|-------------|
| 428 | Nginx sample fails — upstream host not found | Nginx can't resolve upstream service names in docker networking |
| 714 | Deploy uses Dockerfile even when not specified | Picks up Dockerfile from project dir even when not configured |
| 726 | Volume mounts not working with .csproj projects | Files not mounted in container for project-based services |
| 1212 | Containers not removed on build failure | Containers left running when build fails |
| 1255 | Podman can't inject env vars into Postgres container | Env vars not passed with `-e` flag when using Podman |
| 1256 | Podman volume format error | Windows-style paths rejected by Podman |
| 1491 | Volume mounts ignored on proxy containers | Additional volumes not applied through proxy container |

### Ingress / Proxy

| # | Title | Description |
|---|-------|-------------|
| 691 | Authorization header lost in service-to-service communication | Bearer token dropped during proxy redirect |
| 994 | Tye ingress caches cookies incorrectly | Cookies shared across browsers |
| 1078 | UDP not supported in proxies | Can't proxy UDP bindings |
| 1093 | Ingress WebSocket connections fail | WebSocket through ingress doesn't connect |
| 1100 | Can't bind ingress to port 80 | Permission denied with no guidance |
| 1261 | Nginx ingress returns 413 for large payloads | No configurable body size limit |
| 1507 | HTTP/2 cookie headers not transformed for HTTP/1.1 | Multiple Cookie headers forwarded as-is |

### Build / Watch

| # | Title | Description |
|---|-------|-------------|
| 544 | Watch doesn't work with containerized projects | Watch mode doesn't call `dotnet watch` for Docker projects |
| 741 | Watch doesn't detect Blazor WASM changes | File changes don't trigger rebuild |
| 1010 | Watch doesn't reload on Razor file changes | `.cshtml` modifications don't trigger reload |
| 1307 | `build: false` and `--no-build` flags ignored | Build still triggers despite skip flags |
| 1404 | Watch rebuilds in Debug despite Release buildProperties | Ignores Configuration setting |
| 1438 | Watch fails for projects with dots in name | MSBuild error with `.` in project names |

### Configuration / Parsing

| # | Title | Description |
|---|-------|-------------|
| 422 | ConnectionString ENV leaks to all containers | Env vars for one service injected into all containers |
| 841 | ConnectionString not resolved with named binding | `GetConnectionString()` returns null with named bindings |
| 1012 | Dockerfile build ignores dockerFile config value | Always looks for `Dockerfile` regardless of setting |
| 1033 | YAML parsing errors always report "tye.yaml" | Wrong filename in error messages |
| 1419 | Multiple env_file entries don't override | Duplicates created instead of overriding |
| 1515 | Binding routes missing from tye-schema.json | `routes` works but missing from schema |
| 1534 | Environment variables not parsed in volume mapping | `${MY_VAR}` not expanded in paths |
| 1585 | Tye ignores ASPNETCORE_ENVIRONMENT | Only sets DOTNET_ENVIRONMENT |

### Kubernetes / Deploy

| # | Title | Description |
|---|-------|-------------|
| 562 | Error when deploying with HTTPS bindings | Deploy fails with HTTPS protocol bindings |
| 636 | Deploy produces ImagePullBackOff | Images get ImagePullBackOff despite successful push |
| 679 | Ingress deploy fails with admission webhook error | Wrong API version rejected by nginx webhook |
| 846 | Secret validation fails for named bindings | Deploy fails when bindings have explicit names |
| 869 | Command line args missing from K8s manifest | `args` not included in generated deployment |
| 872 | Exception when running tye undeploy | Unhandled exception during undeploy |
| 913 | Ingress uses deprecated v1beta1 API | Fails on newer k8s clusters |
| 1016 | Deploy fails when binding names differ from "http" | Crashes with non-standard binding names |
| 1371 | Misleading error when kubectl fails | Any failure reported as "not installed" |
| 1381 | Deploy fails when username contains quote | Single quote in username breaks MSBuild publish |
| 1430 | Zipkin secret created in wrong namespace | Created in default instead of target namespace |
| 1475 | Deploy tries to build when only images specified | Compiles projects even with image-only config |
| 1544 | Deploy fails with restricted namespace access | `cluster-info` queries kube-system, fails on restricted namespaces |

### Dapr

| # | Title | Description |
|---|-------|-------------|
| 790 | Dapr can't read state from different service | Cross-service state store access fails |
| 961 | Dapr extension doesn't set app-protocol for gRPC | gRPC service invocation fails |
| 1320 | Dapr uses HTTP port even with app-ssl: true | Passes HTTP port instead of HTTPS |
| 1372 | Dapr fails with Dockerfile-defined services | Connection refused for Dockerfile-based services |
| 1393 | Dapr sidecar not created for HTTPS services | Disabled with "unbound service" message |
| 1429 | DAPR_HTTP_PORT and DAPR_GRPC_PORT not set for .NET 6 | Port env vars not injected |
| 1431 | Dapr extension ignores Azure Function apps | No sidecar started for azureFunction services |
| 1500 | Dapr per-service args throw YAML parse error | YAML parsing fails for service-level Dapr args |
| 1603 | Dapr uses deprecated `--components-path` flag | Should use `--resources-path` for Dapr 1.11+ |

### Miscellaneous

| # | Title | Description |
|---|-------|-------------|
| 173 | Executable service causes EventPipe error | EventPipe debug error with `executable` type |
| 762 | SSL error on Windows in debugger mode | HttpRequestException SSL connection failures |
| 890 | Random NullReferenceException in EventPipeEventSource | Crashes during EventPipeEventSource.Dispose |
| 925 | Tye ignores global.json SDK version | Wrong SDK used for build |
| 1002 | `tye run` command not recognized | Subcommand fails in certain builds |
| 1125 | Dashboard "clear logs" doesn't delete logs | Only hides them; they return on navigation |
| 1138 | Exit code not logged properly | Logs object type instead of exit code number |
| 1140 | Liveness probe kills container during debugging | Container restarted while debugger attached |
| 1143 | External repo with Dockerfile in subfolder fails | Dockerfile in subdirectory not found |
| 1202 | Intermittent "metadata file not found" on startup | Random failure requiring retries |
| 1316 | Azure Functions args with --port causes duplicate | Port argument appears twice |
| 1387 | Tye uses wrong Azure Function Core Tools version | Wrong `func` executable from PATH |
| 1547 | NullReferenceException on EventCounters for .NET 7 | Error processing hosting counters |

---

## Enhancements (45)

### Service Lifecycle

| # | Title | Description |
|---|-------|-------------|
| 260 | First-class sidecar support in hosting | Per-replica sidecar launching with service params |
| 607 | Auto-assign port when only protocol is specified | Set autoAssignPort when only `protocol: https` given |
| 791 | Disable/relax health checks in debug mode | Don't restart services while debugger attached |
| 957 | Configure max restart attempts for unhealthy replicas | No way to limit infinite restart loop |
| 1340 | Add enabled/disabled flag for services | Disable services without removing from yaml |
| 1415 | Command to stop leftover containers | Clean up containers surviving host reboot |

### Configuration / Validation

| # | Title | Description |
|---|-------|-------------|
| 310 | Token replacement in Dapr extension command lines | Extend token replacement to Dapr args |
| 380 | Clarify proxy container naming in logs | Proxy containers look like service containers |
| 461 | Improve diagnostics when projects are skipped | Print why projects were included/skipped |
| 772 | .env nested variable references not resolved | `${BASENAME}` references within .env not expanded |
| 937 | Make Elastic extension ports configurable | Ports hardcoded; should allow overrides |

### Docker

| # | Title | Description |
|---|-------|-------------|
| 320 | Support container tools package for building images | VS container tools MSBuild as alternative |
| 348 | Improve Dockerfile generation layering | NMica-style layering for efficiency |
| 655 | Auto-create configured docker network | Create specified network instead of warning |
| 684 | Add `--no-cache` option for docker build | Build without cache for debugging |
| 711 | Stream container logs to tye console | Docker stdout/stderr visible in terminal |
| 715 | Allow container image tag in tye.yaml | Override image tag beyond MSBuild Version |
| 1044 | Use host.docker.internal on Linux Docker 20.10+ | Now supported on Linux |
| 1275 | `tye run --docker` should respect dockerFile config | Ignores dockerFile and generates its own |

### Kubernetes / Deploy

| # | Title | Description |
|---|-------|-------------|
| 126 | Content-based versioning for images | Same tag reused; k8s doesn't pick up changes |
| 695 | Add X-Forwarded headers to ingress | Missing proxy headers for correct redirects |
| 715 | Allow container image tag specification | Override image tag in tye.yaml |
| 737 | Update Ingress to networking.k8s.io/v1 | Upgrade from deprecated v1beta1 |
| 743 | Audit Port vs ContainerPort usage | Inconsistent usage across codebase |
| 758 | ContainerPort should default to 80 for images | Inconsistent between local run and k8s |
| 832 | Allow ingress deploy without interactive prompt | `--no-prompt` flag for CI/CD |

### Dashboard / UX

| # | Title | Description |
|---|-------|-------------|
| 10 | Name sanitization utilities | Legal C# identifiers and DNS names from project names |
| 56 | Clean up console output | Inconsistent output; broken emoji on Windows |
| 204 | Ingress should allow picking specific named binding | Always picks first HTTP binding |
| 257 | Session affinity for ingress | Needed for SignalR/Blazor Server |
| 694 | Dashboard should support structured logging | No structured JSON log parsing |
| 753 | Show Dockerfile path in dashboard | Shows csproj but not Dockerfile path |
| 759 | Debug mode should output PID to attach to | Print exact PID for debugger |
| 1113 | Colored console output option | No color coding in output |
| 1127 | Tye host metadata endpoint in dashboard API | Version/metadata for tooling |
| 1444 | Dashboard quality-of-life improvements | Color-coded status, icon buttons, labels |

### Testing / Internal

| # | Title | Description |
|---|-------|-------------|
| 797 | `tye clean` command | Remove orphaned processes/containers/temp files |
| 1148 | Make unit tests more robust | Random Task canceled / Connection refused on Linux |
| 1208 | Fix E2E test hangs on Linux | Tests hang on Ubuntu and sometimes Windows |
| 1449 | Cache ProjectMetadata evaluation results | Slow evaluation on large solutions |

---

## Feature Requests (78)

### Core Features

| # | Title | Description |
|---|-------|-------------|
| 13 | Daemon/watch mode for tye run | Persistent host with services dropping in/out |
| 20 | Handle jobs that run to completion | Short-lived jobs restarted endlessly |
| 143 | Run as a Windows Service | Non-container production use |
| 223 | Replacement tokens in config values | Resolve env var tokens in configuration |
| 230 | Support multi-targeted projects | Specify which TFM to use |
| 443 | `depends_on` and health checks for startup ordering | Control service startup order |
| 446 | Docker-compose `command` equivalent | Override container entrypoint |
| 597 | Variable substitution syntax in tye.yaml | User-defined variables and patterns |
| 624 | `tye run --detach` background mode | Run tye as background process |
| 652 | Startup order with depends_on / readiness probes | Start after dependencies are healthy |
| 876 | Start/stop/restart individual services at runtime | Per-service control while running |
| 985 | Watch should restart on tye.yaml edits | Config changes trigger restart |

### Docker

| # | Title | Description |
|---|-------|-------------|
| 605 | Override existing Dockerfile per service | Alternative Dockerfile when one exists |
| 649 | Docker container memory limiting | Memory limits per service |
| 653 | Multiple tye instances concurrently | Run two instances with different tags |
| 656 | Multiple Docker networks per service | Attach to more than one network |
| 657 | Docker-compose-like env var syntax | `KEY=VALUE` shorthand and env_file interpolation |
| 717 | Dockerfile on deploy (not just run) | `dockerFile` should work for deploy |
| 1168 | Multi-architecture image builds | Build for arm64 etc. |
| 1382 | Docker `--platform` flag support | Specify target platform |
| 1492 | Docker network aliases | Custom DNS resolution for services |

### Kubernetes / Deploy

| # | Title | Description |
|---|-------|-------------|
| 34 | Default to local Docker registry | `local` registry keyword |
| 398 | "Virtual" services pointing to K8s services | Reference existing services without launching |
| 637 | Init container support | K8s init containers in manifests |
| 645 | ConfigMap and Secrets volume mount support | Mount ConfigMaps/Secrets as volumes |
| 673 | Deploy single service independently | Deploy individual services |
| 719 | Custom imagePullPolicy for K8s | `IfNotPresent` for local dev |
| 732 | Deployment labels for rolling updates | Custom labels to trigger redeployment |
| 754 | Regex in ingress path rules | Regex patterns instead of prefix only |
| 796 | Deploy ingress as separate command | Independent ingress deployment |
| 905 | Set version on build/push via CLI | Pass assembly version and image tag |
| 969 | `tye generate` output manifests to files | Generate YAML to output directory |
| 1051 | AAD Pod Managed Identity label | `aadpodidbinding` label for AKS |
| 1073 | K8s resource limits and requests | CPU/memory limits in tye.yaml |
| 1217 | Deploy services to multiple namespaces | Per-service namespace override |
| 1225 | Custom label support for K8s | Labels for Istio `app`/`version` etc. |
| 1365 | Header injection for ingress | Custom headers on ingress requests |
| 1460 | Merge ingresses with same name from includes | Included files should merge |

### Configuration

| # | Title | Description |
|---|-------|-------------|
| 30 | Secret store for local dev passwords | Store and inject local dev secrets |
| 190 | Import docker-compose format | Convert docker-compose files to tye.yaml |
| 238 | `tye init` globbing support | `tye init **/*.csproj` |
| 377 | Replacement tokens in environment variables | `${binding:service:host}` syntax |
| 413 | Environment-specific tye.yaml files | `tye.production.yaml` pattern |
| 530 | Convert docker-compose to tye format | CLI translation command |
| 574 | Parse secrets from `dotnet user-secrets` | Inject user-secrets into services |
| 606 | `tye add` command | Add projects to existing tye.yaml via CLI |
| 671 | JSON configuration format (tye.json) | Alternative to YAML |
| 676 | Root-level `env` section | Global environment variables |
| 853 | `--exclude` flag for tye run | Exclude services without tags |
| 861 | Parse env var references in env_file | `${ENV_VAR}` substitution in .env |
| 1292 | Environment variables in project paths | `${ENV_VAR}` in project field |
| 1567 | `--watch` targeting specific services | Watch only selected services |
| 1588 | Default tag for auto-run and `tye services` command | Default services and listing command |

### Non-.NET / Frontend

| # | Title | Description |
|---|-------|-------------|
| 546 | Auto-detect JavaScript SPAs | React/Angular/Vue project recognition |
| 665 | Run frontend commands as services | `npm start` as first-class service |
| 873 | Shell commands for JavaScript projects | npm/yarn as service executables |
| 1607 | Support `.esproj` JavaScript projects | `.esproj` in project field |

### Dapr

| # | Title | Description |
|---|-------|-------------|
| 1065 | `--app-ssl` flag for Dapr extension | Dapr sidecar with app-ssl config |
| 1097 | Dapr sidecars for non-HTTP services | Pub-sub only workers excluded |

### Observability / API

| # | Title | Description |
|---|-------|-------------|
| 356 | Query replica addresses via service API | Expose replica addresses for load-balancing |
| 672 | Log auto-scroll toggle in dashboard | Toggleable auto-scroll |
| 1052 | Access services from remote devices on LAN | Non-localhost binding for mobile testing |
| 1087 | Custom certificates on HTTPS bindings | Custom TLS certificate for ingress |
| 1170 | Webhook/callback for replica lifecycle events | Notifications on replica create/stop/restart |
| 1181 | Streaming logs endpoint | SignalR/WebSocket real-time log streaming |
| 1504 | Multiple "apps" (namespaced service groups) | Logical app groups in single config |

### Multi-Environment

| # | Title | Description |
|---|-------|-------------|
| 46 | Multi-service targeting remote and local | Local + remote proxy hybrid |
| 397 | Helm chart-based services | Complex stateful services via Helm |
