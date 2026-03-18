# Code Issues — Scan Results

## CRITICAL

### 1. Deadlock risk — `.Result` on async calls
- **File**: `src/Tye2.Core/ContainerEngine.cs` (lines 97, 146)
- `ProcessUtil.RunAsync(...).Result` blocks the thread synchronously. Can deadlock if called from a sync context with a captured `SynchronizationContext`.

## HIGH

### 2. Array index out of bounds — `Split()` without bounds checks
- `src/Tye2.Proxy/Program.cs` (lines 38-39) — `portValue.Split(':')[1]` with no length check
- ~~`src/Tye2.Core/ApplicationFactory.cs` (lines 129-130, 169-170)~~ — **Safe**: uses `Split(':', 3)` with count parameter
- `src/Tye2.Core/DockerCompose/DockerComposeParser.cs` (line ~290) — `Split('=')` without bounds check on env var parsing
- ~~`src/Tye2.Core/DockerCompose/DockerComposeParser.cs` (lines 323-325) — port `Split(':')`~~ — **Fixed**: uses `switch (parts.Length)` for safe access

### 3. Empty YAML crashes
- `src/Tye2.Core/Serialization/YamlParser.cs` (line 65)
- `src/Tye2.Core/DockerCompose/DockerComposeParser.cs` (line 50)
- `_yamlStream.Documents[0]` accessed without checking if `Documents` is empty — `IndexOutOfRangeException` on empty/malformed YAML files.

### 4. ~~Resource leak — Process not disposed~~ — Mostly safe
- **File**: `src/Tye2.Core/ProcessExtensions.cs` (line 109)
- Process is handled with null-coalescing operators and `WaitForExit()`/`Kill()`. Not explicitly disposed but unlikely to leak in practice.

### 5. DNS empty result
- **File**: `src/Tye2.Core/ContainerEngine.cs` (line 160)
- `Dns.GetHostAddresses(...).Last()` throws `InvalidOperationException` if DNS returns empty.

## MEDIUM

### 6. Command injection via URL
- **File**: `src/Tye2.Hosting/TyeHost.cs` (line 377)
- `$"/c start {url}"` — URL is not quoted/escaped before passing to shell. Note: `&` is escaped to `^&` but no full quoting.

### 7. Exception swallowing
- **File**: `src/Tye2.Core/ProcessExtensions.cs` (lines 76-79, 92-95)
- `Win32Exception` caught based on string message matching — fragile and locale-dependent.

## Recommended Fix Order

1. **ContainerEngine.cs `.Result` calls** — highest risk of real-world deadlocks. Convert to proper async or use `GetAwaiter().GetResult()` with `ConfigureAwait(false)`.
2. **Split bounds checks** — remaining unguarded `Split()` calls in Proxy and DockerComposeParser env var parsing.
3. **Empty YAML guard** — trivial fix, prevents a confusing crash on empty/malformed YAML files.
4. **DNS empty result guard** — add `FirstOrDefault()` or empty check before `Last()`.
5. **URL quoting** — wrap URL in quotes in shell command.

## Fixed / No Longer Applicable (as of 2026-03-15)

- [x] ApplicationFactory `Split(':', 3)` — uses count parameter, safe
- [x] DockerComposeParser port parsing — refactored to use `switch (parts.Length)`
- [x] ProcessExtensions process leak — process is properly handled with null checks and WaitForExit
