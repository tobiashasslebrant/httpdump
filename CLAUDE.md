# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Single ASP.NET Core project (`httpdump/`, net10.0, solution at repo root). There are no tests or linters configured.

```bash
dotnet build httpdump.sln
dotnet run --project httpdump          # see httpdump/Properties/launchSettings.json for port
docker build -t httpdump .             # run from repo root (Dockerfile copies httpdump/)
```

See `README.md` for configuration settings, endpoints and Docker run examples.

## Architecture

Everything is wired in `httpdump/Program.cs` with a single inline middleware (no controllers are actually mapped, despite `AddControllers()`). The middleware:

1. Serves `GET /httpdump` (route index, `HelpPage.cs`), `/httpdump/ui` (static HTML string in `UiPage.cs`) and `/httpdump/config` (GET/POST) directly. `HelpPage.cs` is a hand-written route list — update it when adding routes.
2. Handles `/httpdump/response[/{id}]` (override CRUD: GET list, POST add, PUT replace, DELETE remove), then, for any non-`/httpdump` path, checks `ResponseOverrideStore` (singleton, in-memory, `*` wildcards → regex) and short-circuits with the canned response on a match.
3. Otherwise dispatches to **`ReverseProxyHandler`** if `RuntimeConfig.IsReverseProxy` (a non-empty `ReverseProxyHost`), else to **`DumpHandler`**. Both handle `/httpdump/cache` and `/httpdump/clear` themselves and treat every other path as traffic.

Before any of this, the middleware enforces `AdminAuth` (basic auth) on every `/httpdump*` path when both `HTTPDUMP_USER` and `HTTPDUMP_PWD` are set; non-`/httpdump` traffic is never authenticated.

Key points that span files:

- **`RuntimeConfig`** (singleton, lock-guarded) holds the mutable proxy settings (host, port, scheme, cache-enabled), initialised from configuration/env vars and changed at runtime via `POST /httpdump/config`. `AppConfig` is a thin façade over it plus `PostgresConnectionString`, which is read from `IConfiguration` only (not runtime-changeable). Changing config only clears the *in-memory* cache (`inMemoryCache.Clear()` in `Program.cs`), not Postgres.
- **Request identity**: `MD5Hasher.Hash(RequestInfo)` hashes the JSON-serialised request (`Models/RequestInfoFactory`). The hash is the cache key in both modes. Dump mode counts duplicates in a private `List` inside `DumpHandler`; proxy mode stores `ResponseCacheItem(hash, request, response)`.
- **Reverse proxy flow** (`ReverseProxy/ReverseProxy.cs` + `ReverseProxyHandler`): on cache hit, `ResponseWriter` replays the stored `ResponseInfo`; on miss, the request is forwarded via the named `HttpClientWithSSLUntrusted` client (certificate validation disabled, auto-decompression on), streamed to the caller, then stored when `ReverseProxyCacheEnabled`.
- **Cache backends** behind `IResponseCache` (`Cache/`): `InMemoryResponseCache` (singleton) or `PostgresCache`, chosen per request by `ResponseCacheFactory` based on whether `PostgresConnectionString` is set. `PostgresCache` creates its tables lazily (`tbl_httpdump_*`) and builds SQL via string interpolation (not parameterised) — keep this in mind when touching it.
- `DumpHandler` has its own in-handler list and is a transient service resolved once at startup in `Program.cs`, so its state lives for the app's lifetime despite the transient registration.
- **Response overrides** (`ResponseOverrideStore`, `Models/ResponseOverride.cs`): match on method, protocol, host, relative URL, query params, headers and body, with `*` wildcards (empty/omitted fields and empty-key entries match anything; newest match wins). `Program.cs` calls `EnableBuffering()` on every request because the body is read several times (hashing, override matching, proxying) — `StreamExtensions.DeepCopy` relies on that. The UI (`UiPage.cs`) has a panel for listing/adding/editing/deleting overrides.
