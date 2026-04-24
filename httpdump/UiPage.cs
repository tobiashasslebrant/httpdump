namespace HttpDump;

public static class UiPage
{
    public const string Html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>httpdump</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { font-family: system-ui, sans-serif; margin: 0; background: #f5f5f5; color: #222; }
            header { background: #1a1a2e; color: #fff; padding: 1rem 1.5rem; display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; }
            header h1 { margin: 0; font-size: 1.25rem; flex: 1; }
            .toolbar { display: flex; align-items: center; gap: .75rem; flex-wrap: wrap; }
            button { padding: .4rem .9rem; border: none; border-radius: 4px; cursor: pointer; font-size: .875rem; }
            .btn-danger { background: #c0392b; color: #fff; }
            .btn-default { background: #e0e0e0; color: #222; }
            .btn-primary { background: #2980b9; color: #fff; }
            label { font-size: .875rem; display: inline-flex; align-items: center; gap: .3rem; }
            input[type=checkbox] { margin: 0; cursor: pointer; }
            #status { font-size: .8rem; opacity: .7; }
            .mode-badge { font-size: .75rem; padding: .2rem .6rem; border-radius: 10px; font-weight: 600; }
            .mode-dump  { background: #e8f4f8; color: #1a5276; }
            .mode-proxy { background: #e9f7ef; color: #1e8449; }
            main { padding: 1.25rem; max-width: 1200px; margin: auto; }
            #config-panel { background: #fff; border: 1px solid #ddd; border-radius: 6px; padding: 1rem 1.25rem; margin-bottom: 1.25rem; }
            #config-panel h2 { margin: 0 0 .75rem; font-size: 1rem; }
            .config-form { display: flex; gap: .75rem; align-items: flex-end; flex-wrap: wrap; }
            .config-form .field { display: flex; flex-direction: column; gap: .25rem; }
            .config-form input { padding: .375rem .6rem; border: 1px solid #ccc; border-radius: 4px; font-size: .875rem; width: 220px; }
            .config-form select { padding: .375rem .6rem; border: 1px solid #ccc; border-radius: 4px; font-size: .875rem; background: #fff; }
            .config-form input[name=port] { width: 90px; }
            #items { display: flex; flex-direction: column; gap: .5rem; }
            .empty { color: #888; text-align: center; padding: 2rem; background: #fff; border-radius: 6px; border: 1px solid #eee; }
            .item { background: #fff; border: 1px solid #ddd; border-radius: 6px; overflow: hidden; }
            .item-header { display: flex; align-items: center; gap: .75rem; padding: .625rem 1rem; cursor: pointer; user-select: none; }
            .item-header:hover { background: #f9f9f9; }
            .method { font-weight: 700; font-size: .8rem; padding: .2rem .45rem; border-radius: 3px; color: #fff; min-width: 52px; text-align: center; }
            .GET    { background: #27ae60; }
            .POST   { background: #2980b9; }
            .PUT    { background: #f39c12; }
            .PATCH  { background: #8e44ad; }
            .DELETE { background: #c0392b; }
            .method-other { background: #7f8c8d; }
            .status-code { font-size: .8rem; padding: .2rem .4rem; border-radius: 3px; font-weight: 600; }
            .status-2xx { background: #d5f5e3; color: #1e8449; }
            .status-3xx { background: #d6eaf8; color: #1a5276; }
            .status-4xx { background: #fdebd0; color: #935116; }
            .status-5xx { background: #fadbd8; color: #922b21; }
            .path { font-family: monospace; font-size: .9rem; flex: 1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
            .count { font-size: .8rem; color: #888; white-space: nowrap; }
            .chevron { margin-left: auto; font-size: .75rem; color: #aaa; transition: transform .15s; }
            .item.open .chevron { transform: rotate(180deg); }
            .item-body { display: none; border-top: 1px solid #eee; padding: 1rem; }
            .item.open .item-body { display: block; }
            .section { margin-bottom: 1rem; }
            .section h3 { margin: 0 0 .4rem; font-size: .8rem; text-transform: uppercase; letter-spacing: .05em; color: #888; }
            .headers-table { width: 100%; border-collapse: collapse; font-size: .8rem; font-family: monospace; }
            .headers-table td { padding: .25rem .5rem; vertical-align: top; border-bottom: 1px solid #f0f0f0; }
            .headers-table td:first-child { color: #555; white-space: nowrap; width: 35%; }
            pre { margin: 0; background: #f8f8f8; padding: .75rem; border-radius: 4px; font-size: .8rem; overflow-x: auto; white-space: pre-wrap; word-break: break-all; }
          </style>
        </head>
        <body>
        <header>
          <h1>httpdump</h1>
          <div class="toolbar">
            <span id="mode-badge"></span>
            <span id="status"></span>
            <label><input type="checkbox" id="auto-refresh" />Auto-refresh</label>
            <button class="btn-default" onclick="loadCache()">Refresh</button>
            <button class="btn-danger" onclick="clearCache()">Clear cache</button>
          </div>
        </header>
        <main>
          <div id="config-panel">
            <h2>Proxy settings</h2>
            <div class="config-form">
              <div class="field">
                <label><input type="checkbox" id="cfg-enabled" onchange="onProxyToggle()" />Enable reverse proxy</label>
              </div>
              <div class="field">
                <label for="cfg-scheme">Scheme</label>
                <select id="cfg-scheme" disabled onchange="onSchemeChange()">
                  <option value="http">http</option>
                  <option value="https">https</option>
                </select>
              </div>
              <div class="field">
                <label for="cfg-host">Host</label>
                <input id="cfg-host" name="host" type="text" placeholder="e.g. api.example.com" disabled />
              </div>
              <div class="field">
                <label for="cfg-port">Port</label>
                <input id="cfg-port" name="port" type="number" min="1" max="65535" placeholder="80" disabled />
              </div>
              <button class="btn-primary" onclick="saveConfig()">Save</button>
              <div class="field">
                <label><input type="checkbox" id="cfg-cache" checked disabled />Cache responses</label>
              </div>
            </div>
          </div>
          <div id="items"><p class="empty">Loading…</p></div>
        </main>
        <script>
          let autoRefreshTimer = null;

          let currentConfig = {};

          async function loadConfig() {
            try {
              const res = await fetch('/httpdump/config');
              const cfg = await res.json();
              currentConfig = cfg;
              applyConfig(cfg);
            } catch (e) { /* ignore */ }
          }

          function applyConfig(cfg) {
            currentConfig = cfg;
            const enabled = cfg.isReverseProxy === true;
            document.getElementById('cfg-enabled').checked = enabled;
            document.getElementById('cfg-scheme').value = cfg.reverseProxyScheme || 'http';
            document.getElementById('cfg-host').value = cfg.reverseProxyHost || '';
            document.getElementById('cfg-port').value = cfg.reverseProxyPort || 80;
            document.getElementById('cfg-cache').checked = cfg.reverseProxyCacheEnabled !== false;
            document.getElementById('cfg-scheme').disabled = !enabled;
            document.getElementById('cfg-host').disabled = !enabled;
            document.getElementById('cfg-port').disabled = !enabled;
            document.getElementById('cfg-cache').disabled = !enabled;
            const badge = document.getElementById('mode-badge');
            badge.textContent = enabled ? 'Reverse proxy' : 'Dump mode';
            badge.className = 'mode-badge ' + (enabled ? 'mode-proxy' : 'mode-dump');
          }

          function onProxyToggle() {
            const enabled = document.getElementById('cfg-enabled').checked;
            document.getElementById('cfg-scheme').disabled = !enabled;
            document.getElementById('cfg-host').disabled = !enabled;
            document.getElementById('cfg-port').disabled = !enabled;
            document.getElementById('cfg-cache').disabled = !enabled;
          }

          function onSchemeChange() {
            const scheme = document.getElementById('cfg-scheme').value;
            const portEl = document.getElementById('cfg-port');
            const defaultPort = scheme === 'https' ? 443 : 80;
            if (portEl.value == 80 || portEl.value == 443) {
              portEl.value = defaultPort;
            }
          }

          async function saveConfig() {
            const enabled = document.getElementById('cfg-enabled').checked;
            const scheme = document.getElementById('cfg-scheme').value || 'http';
            const host = enabled ? document.getElementById('cfg-host').value.trim() : '';
            const port = parseInt(document.getElementById('cfg-port').value, 10) || 80;
            const cacheEnabled = document.getElementById('cfg-cache').checked;
            if (enabled && !host) { alert('Host is required when reverse proxy is enabled'); return; }
            if (enabled && (port < 1 || port > 65535)) { alert('Port must be between 1 and 65535'); return; }
            try {
              const res = await fetch('/httpdump/config', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reverseProxyHost: host, reverseProxyPort: port, reverseProxyScheme: scheme, reverseProxyCacheEnabled: cacheEnabled })
              });
              if (!res.ok) { alert('Failed to save config: ' + await res.text()); return; }
              const cfg = await res.json();
              applyConfig(cfg);
              setStatus('Config saved');
              loadCache();
            } catch (e) { alert('Error: ' + e.message); }
          }

          async function loadCache() {
            try {
              const res = await fetch('/httpdump/cache');
              const items = await res.json();
              renderItems(items);
              setStatus('Updated ' + new Date().toLocaleTimeString());
            } catch (e) {
              setStatus('Error: ' + e.message);
            }
          }

          async function clearCache() {
            await fetch('/httpdump/clear');
            loadCache();
          }

          function renderItems(items) {
            const container = document.getElementById('items');
            if (!items || items.length === 0) {
              container.innerHTML = '<p class="empty">No cached items</p>';
              return;
            }
            container.innerHTML = '';
            items.forEach((item, i) => {
              const el = document.createElement('div');
              el.className = 'item';
              el.innerHTML = buildItemHtml(item, i);
              el.querySelector('.item-header').addEventListener('click', () => el.classList.toggle('open'));
              container.appendChild(el);
            });
          }

          function buildItemHtml(item, i) {
            const isProxy = item.responseInfo != null;
            const req = isProxy ? item.requestInfo : item.item;
            const method = safeText(req?.requestMethod ?? '?');
            const path = safeText(req?.requestPath ?? '/');
            const qs = safeText((req?.queryStringValue ?? '').replace(/^\?/, ''));
            const fullPath = qs ? path + '?' + qs : path;

            let displayUrl = fullPath;
            if (isProxy && currentConfig.reverseProxyHost) {
              const scheme = currentConfig.reverseProxyScheme || 'http';
              const port = currentConfig.reverseProxyPort;
              const defaultPort = scheme === 'https' ? 443 : 80;
              const portSuffix = port === defaultPort ? '' : `:${port}`;
              displayUrl = `${scheme}://${currentConfig.reverseProxyHost}${portSuffix}${fullPath}`;
            }

            const count = item.count;
            const methodClass = ['GET','POST','PUT','PATCH','DELETE'].includes(method) ? method : 'method-other';

            let statusBadge = '';
            if (isProxy) {
              const code = item.responseInfo?.statusCode ?? 0;
              const cls = code >= 500 ? 'status-5xx' : code >= 400 ? 'status-4xx' : code >= 300 ? 'status-3xx' : 'status-2xx';
              statusBadge = `<span class="status-code ${cls}">${code}</span>`;
            }

            const countBadge = count != null ? `<span class="count">×${count}</span>` : '';

            return `
              <div class="item-header">
                <span class="method ${methodClass}">${method}</span>
                <span class="path">${displayUrl}</span>
                ${statusBadge}
                ${countBadge}
                <span class="chevron">▼</span>
              </div>
              <div class="item-body">${buildDetailHtml(item, isProxy, req)}</div>`;
          }

          function buildDetailHtml(item, isProxy, req) {
            const headers = req?.headers ?? {};
            const body = req?.body ?? '';
            let html = '';

            html += '<div class="section"><h3>Request headers</h3>';
            html += '<table class="headers-table">';
            for (const [k, v] of Object.entries(headers)) {
              const vals = Array.isArray(v) ? v.join(', ') : v;
              html += `<tr><td>${safeText(k)}</td><td>${safeText(vals)}</td></tr>`;
            }
            html += '</table></div>';

            if (body) {
              html += `<div class="section"><h3>Request body</h3><pre id="body-${item.hash}"></pre></div>`;
            }

            if (isProxy) {
              const resp = item.responseInfo;
              const contentType = safeText(resp?.contentType ?? '');
              html += `<div class="section"><h3>Response — ${contentType}</h3><pre id="resp-${item.hash}"></pre></div>`;
              html += `<script>document.getElementById('resp-${item.hash}').textContent=${JSON.stringify(resp?.content ?? '')};<\/script>`;
            }

            if (body) {
              html += `<script>document.getElementById('body-${item.hash}').textContent=${JSON.stringify(body)};<\/script>`;
            }

            return html;
          }

          function safeText(str) {
            const d = document.createElement('div');
            d.textContent = String(str);
            return d.innerHTML;
          }

          function setStatus(msg) {
            document.getElementById('status').textContent = msg;
          }

          document.getElementById('auto-refresh').addEventListener('change', e => {
            clearInterval(autoRefreshTimer);
            if (e.target.checked) autoRefreshTimer = setInterval(loadCache, 3000);
          });

          loadConfig();
          loadCache();
        </script>
        </body>
        </html>
        """;
}
