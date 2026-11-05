# httpdump

A lightweight ASP.NET Core HTTP debugging and mocking tool. Useful for inspecting outgoing HTTP requests or stubbing upstream services during development and testing.

## Modes

### 1. Dump mode (default)

Accepts any HTTP request, logs it to an in-memory cache, and returns a confirmation. Use this to inspect what a client is sending.

### 2. Reverse proxy mode

Enabled by setting `ReverseProxyHost`. Forwards incoming requests to the configured upstream service, caches the response, and replays the cached response on subsequent identical requests.

## Configuration

Set via `appsettings.json` or environment variables:

| Setting                  | Description                                              | Default |
|--------------------------|----------------------------------------------------------|---------|
| `ReverseProxyHost`       | Upstream host to proxy to. Enables reverse proxy mode when set. | _(empty — dump mode)_ |
| `ReverseProxyPort`       | Upstream port                                            | `80`    |
| `PostgresConnectionString` | If set, uses PostgreSQL as the response cache instead of in-memory | _(empty — in-memory)_ |
| `HTTPDUMP_USER`          | Basic auth user for all `/httpdump*` routes. Auth is enabled only if **both** `HTTPDUMP_USER` and `HTTPDUMP_PWD` are set | _(empty — no auth)_ |
| `HTTPDUMP_PWD`           | Basic auth password for all `/httpdump*` routes          | _(empty — no auth)_ |

## Securing the admin routes

If both `HTTPDUMP_USER` and `HTTPDUMP_PWD` environment variables are set, every `/httpdump*` route (UI, cache, clear, config, response overrides, route index) requires HTTP basic auth. All other paths (the traffic being dumped or proxied) are never authenticated. If only one of the two is set, auth stays disabled and a warning is logged at startup. Basic auth sends credentials unencrypted, so put httpdump behind TLS when exposing it.

```bash
docker run -p 8080:80 -e HTTPDUMP_USER=admin -e HTTPDUMP_PWD='<secret>' httpdump
```

## Web UI

Navigate to **`/httpdump/ui`** for a browser-based interface that lets you:

- Browse all cached requests/responses in an expandable list
- View request headers, body, response status, and content
- Clear the cache with one click
- Toggle auto-refresh (every 3 seconds)
- **Reverse proxy mode only:** edit the upstream host and port at runtime without restarting

## Endpoints

| Endpoint              | Method | Description                                              |
|-----------------------|--------|----------------------------------------------------------|
| `/httpdump/ui`        | GET    | Web UI                                                   |
| `/httpdump/cache`     | GET    | Raw cache as JSON                                        |
| `/httpdump/clear`     | GET    | Clear the cache                                          |
| `/httpdump/config`    | GET    | Current reverse proxy config `{ isReverseProxy, reverseProxyHost, reverseProxyPort }` |
| `/httpdump/config`    | POST   | Update reverse proxy host/port at runtime (clears cache) |
| `/httpdump/response`  | GET    | List response overrides                                  |
| `/httpdump/response`  | POST   | Add a response override (returns it with its `id`)       |
| `/httpdump/response/{id}` | PUT    | Replace an override (same body as POST, keeps its `id`)  |
| `/httpdump/response/{id}` | DELETE | Remove a response override                           |

### POST /httpdump/config

```json
{ "reverseProxyHost": "api.example.com", "reverseProxyPort": 443 }
```

Returns the updated config as JSON. Clears the response cache so stale entries from the previous upstream are not replayed.

## Response overrides

`POST /httpdump/response` registers a canned response that takes priority over dump/proxy handling (and the cache) for any matching request. Overrides are in-memory only and lost on restart. If several match, the most recently added wins.

```json
{
  "name": "optional, defaults to <protocol><baseUrl>/<relativeUrl>?<query>",
  "request": {
    "match": {
      "method": "GET",
      "protocol": "http",
      "baseUrl": "mydomain.com",
      "relativeUrl": "users/*",
      "queryParameters": [{ "key": "name", "value": "*" }],
      "ignoreQueryParameters": false,
      "headers": [{ "key": "X-Env", "value": "*" }],
      "body": ""
    }
  },
  "response": {
    "statusCode": 200,
    "headers": [{ "key": "Content-Type", "value": "application/json" }],
    "body": "{\"ok\":true}"
  }
}
```

- `*` matches any characters in `baseUrl`, `relativeUrl`, query values, `body`, `method` and `protocol`. Omitted/empty fields match anything.
- `baseUrl` is compared to the request `Host` header (with or without port); host/protocol/method are case-insensitive.
- Every listed query parameter must be present in the request (extra parameters are allowed); `ignoreQueryParameters: true` skips query matching.
- Every listed header must be present (name case-insensitive) with at least one value matching the pattern. Entries with an empty `key` are ignored.
- `statusCode` defaults to 200 and `method` is optional. Must be valid JSON (comments and trailing commas are tolerated).

## Running with Docker

```bash
# Dump mode
docker run -p 8080:80 httpdump

# Reverse proxy mode
docker run -p 8080:80 \
  -e ReverseProxyHost=my-service \
  -e ReverseProxyPort=3000 \
  httpdump

# Dump mode with basic auth on the admin routes (/httpdump*)
docker run -p 8080:80 \
  -e HTTPDUMP_USER=admin \
  -e HTTPDUMP_PWD='<secret>' \
  httpdump

# Reverse proxy with PostgreSQL cache
docker run -p 8080:80 \
  -e ReverseProxyHost=my-service \
  -e PostgresConnectionString="Host=db;Database=httpdump;Username=user;Password=pass" \
  httpdump
```

## Building

```bash
dotnet build httpdump.sln
# or
docker build -t httpdump .
```
