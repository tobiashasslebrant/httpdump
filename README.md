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

### POST /httpdump/config

```json
{ "reverseProxyHost": "api.example.com", "reverseProxyPort": 443 }
```

Returns the updated config as JSON. Clears the response cache so stale entries from the previous upstream are not replayed.

## Running with Docker

```bash
# Dump mode
docker run -p 8080:80 httpdump

# Reverse proxy mode
docker run -p 8080:80 \
  -e ReverseProxyHost=my-service \
  -e ReverseProxyPort=3000 \
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
