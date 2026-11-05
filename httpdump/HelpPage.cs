namespace HttpDump;

public static class HelpPage
{
    public const string Html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>httpdump – routes</title>
          <style>
            body { font-family: system-ui, sans-serif; margin: 0; background: #f5f5f5; color: #222; }
            header { background: #1a1a2e; color: #fff; padding: 1rem 1.5rem; }
            header h1 { margin: 0; font-size: 1.25rem; }
            main { padding: 1.25rem; max-width: 1000px; margin: auto; }
            p.lead { color: #555; }
            table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #ddd; border-radius: 6px; overflow: hidden; margin-bottom: 1.25rem; }
            th, td { text-align: left; padding: .55rem .75rem; border-bottom: 1px solid #eee; vertical-align: top; font-size: .875rem; }
            th { background: #fafafa; font-size: .75rem; text-transform: uppercase; letter-spacing: .05em; color: #888; }
            code { font-family: monospace; background: #f0f0f0; padding: .1rem .3rem; border-radius: 3px; }
            .m { font-weight: 700; font-size: .75rem; padding: .15rem .4rem; border-radius: 3px; color: #fff; display: inline-block; min-width: 3.2rem; text-align: center; }
            .GET { background: #27ae60; } .POST { background: #2980b9; } .PUT { background: #f39c12; } .DELETE { background: #c0392b; } .ANY { background: #7f8c8d; }
            h2 { font-size: 1rem; margin: 1.5rem 0 .5rem; }
            a { color: #2980b9; }
          </style>
        </head>
        <body>
        <header><h1>httpdump – available routes</h1></header>
        <main>
          <p class="lead">Management routes live under <code>/httpdump</code>. Every other path is treated as traffic to inspect or mock. Open the <a href="/httpdump/ui">web UI</a> for a graphical view. If <code>HTTPDUMP_USER</code> and <code>HTTPDUMP_PWD</code> are set, all <code>/httpdump*</code> routes require HTTP basic auth.</p>

          <h2>Management</h2>
          <table>
            <tr><th>Method</th><th>Route</th><th>Description</th></tr>
            <tr><td><span class="m GET">GET</span></td><td><code>/httpdump</code></td><td>This page.</td></tr>
            <tr><td><span class="m GET">GET</span></td><td><code>/httpdump/ui</code></td><td>Web UI: browse requests/responses, clear the cache, edit proxy settings and manage response overrides.</td></tr>
            <tr><td><span class="m GET">GET</span></td><td><code>/httpdump/cache</code></td><td>Everything captured so far as JSON. Dump mode: received requests with a hit count. Reverse proxy mode: cached request/response pairs.</td></tr>
            <tr><td><span class="m GET">GET</span></td><td><code>/httpdump/clear</code></td><td>Clears the cache and reports how many items were removed.</td></tr>
            <tr><td><span class="m GET">GET</span></td><td><code>/httpdump/config</code></td><td>Current reverse proxy settings: <code>isReverseProxy</code>, <code>reverseProxyHost</code>, <code>reverseProxyPort</code>, <code>reverseProxyScheme</code>, <code>reverseProxyCacheEnabled</code>.</td></tr>
            <tr><td><span class="m POST">POST</span></td><td><code>/httpdump/config</code></td><td>Update the proxy settings at runtime (JSON body with the same fields). An empty host switches to dump mode. Clears the in-memory cache.</td></tr>
          </table>

          <h2>Response overrides</h2>
          <p class="lead">Canned responses that take priority over dump/proxy handling (and the cache) for any matching request. Kept in memory only. If several match, the most recently added wins. See <code>README.md</code> for the full body format; <code>*</code> matches any characters.</p>
          <table>
            <tr><th>Method</th><th>Route</th><th>Description</th></tr>
            <tr><td><span class="m GET">GET</span></td><td><code>/httpdump/response</code></td><td>List all overrides.</td></tr>
            <tr><td><span class="m POST">POST</span></td><td><code>/httpdump/response</code></td><td>Add an override. Body: <code>name</code> (optional), <code>request.match</code> (method, protocol, baseUrl, relativeUrl, queryParameters, ignoreQueryParameters, headers, body) and <code>response</code> (statusCode, headers, body). Returns <code>201</code> with the override and its <code>id</code>.</td></tr>
            <tr><td><span class="m PUT">PUT</span></td><td><code>/httpdump/response/{id}</code></td><td>Replace an override, keeping its <code>id</code> and priority. <code>404</code> if unknown.</td></tr>
            <tr><td><span class="m DELETE">DELETE</span></td><td><code>/httpdump/response/{id}</code></td><td>Remove an override. <code>204</code> on success, <code>404</code> if unknown.</td></tr>
          </table>

          <h2>Everything else</h2>
          <table>
            <tr><th>Method</th><th>Route</th><th>Description</th></tr>
            <tr><td><span class="m ANY">ANY</span></td><td><code>/*</code></td><td>Checked against the response overrides first. Without a match: in dump mode the request is recorded and <code>saved to cache</code> is returned; in reverse proxy mode it is forwarded to the configured upstream (and the response cached/replayed unless caching is disabled).</td></tr>
          </table>
        </main>
        </body>
        </html>
        """;
}
