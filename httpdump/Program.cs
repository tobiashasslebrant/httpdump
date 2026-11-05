using System.Net;
using System.Text.Json;
using HttpDump;
using HttpDump.Cache;
using HttpDump.Models;
using HttpDump.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton<RuntimeConfig>()
    .AddTransient<DumpHandler>()
    .AddTransient<ReverseProxyHandler>()
    .AddTransient<ReverseProxy>()
    .AddTransient<ResponseCacheFactory>()
    .AddTransient<ResponseWriter>()
    .AddTransient<ResponseInfoFactory>()
    .AddTransient<RequestInfoFactory>()
    .AddTransient<AppConfig>()
    .AddTransient<PostgresCache>()
    .AddSingleton<InMemoryResponseCache>();

builder.Services.AddHttpClient("HttpClientWithSSLUntrusted").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });

builder.Services.AddControllers();

var app = builder.Build();

var runtimeConfig = app.Services.GetRequiredService<RuntimeConfig>();
var reverseProxyHandler = app.Services.GetRequiredService<ReverseProxyHandler>();
var dumpHandler = app.Services.GetRequiredService<DumpHandler>();
var inMemoryCache = app.Services.GetRequiredService<InMemoryResponseCache>();
var overrides = app.Services.GetRequiredService<ResponseOverrideStore>();
var adminAuth = app.Services.GetRequiredService<AdminAuth>();
if (!adminAuth.Enabled)
    app.Logger.LogWarning("HTTPDUMP_USER/HTTPDUMP_PWD not both set: /httpdump routes are NOT protected");
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    WriteIndented = true
};

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/httpdump") && !adminAuth.IsAuthorized(context.Request))
    {
        AdminAuth.Challenge(context.Response);
        return;
    }

    context.Request.EnableBuffering(); // body is read more than once (hashing, override matching, proxying)

    if ((path.Equals("/httpdump") || path.Equals("/httpdump/")) && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(HelpPage.Html);
        return;
    }

    if (path.Equals("/httpdump/ui"))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(UiPage.Html);
        return;
    }

    if (path.Equals("/httpdump/config"))
    {
        if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                isReverseProxy = runtimeConfig.IsReverseProxy,
                reverseProxyHost = runtimeConfig.ReverseProxyHost,
                reverseProxyPort = runtimeConfig.ReverseProxyPort,
                reverseProxyScheme = runtimeConfig.ReverseProxyScheme,
                reverseProxyCacheEnabled = runtimeConfig.ReverseProxyCacheEnabled
            }));
            return;
        }

        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Request.Body);
                var host = body.GetProperty("reverseProxyHost").GetString()?.Trim();
                var port = body.GetProperty("reverseProxyPort").GetInt32();
                var scheme = body.TryGetProperty("reverseProxyScheme", out var schemeProp)
                    ? schemeProp.GetString()?.ToLowerInvariant() : "http";
                var cacheEnabled = !body.TryGetProperty("reverseProxyCacheEnabled", out var cacheProp) || cacheProp.GetBoolean();

                if (!string.IsNullOrEmpty(host) && (port < 1 || port > 65535))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("reverseProxyPort must be between 1 and 65535");
                    return;
                }

                runtimeConfig.Update(string.IsNullOrEmpty(host) ? null : host, port, scheme ?? "http", cacheEnabled);
                await inMemoryCache.Clear();

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    isReverseProxy = runtimeConfig.IsReverseProxy,
                    reverseProxyHost = runtimeConfig.ReverseProxyHost,
                    reverseProxyPort = runtimeConfig.ReverseProxyPort,
                    reverseProxyScheme = runtimeConfig.ReverseProxyScheme,
                    reverseProxyCacheEnabled = runtimeConfig.ReverseProxyCacheEnabled
                }));
            }
            catch (Exception)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid JSON body");
            }
            return;
        }
    }

    if (path.StartsWithSegments("/httpdump/response", out var remaining))
    {
        var method = context.Request.Method;
        var id = remaining.Value?.Trim('/');

        if (string.IsNullOrEmpty(id) && HttpMethods.IsGet(method))
        {
            await context.Response.WriteAsJsonAsync(overrides.All(), jsonOptions);
            return;
        }

        if (string.IsNullOrEmpty(id) && HttpMethods.IsPost(method))
        {
            try
            {
                var set = await JsonSerializer.DeserializeAsync<SetResponseRequest>(context.Request.Body, jsonOptions);
                if (set?.Request?.Match is null || set.Response is null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("'request.match' and 'response' are required");
                    return;
                }

                var created = overrides.Add(set.Name, set.Request, set.Response);
                context.Response.StatusCode = 201;
                await context.Response.WriteAsJsonAsync(created, jsonOptions);
            }
            catch (JsonException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid JSON body");
            }
            return;
        }

        if (!string.IsNullOrEmpty(id) && HttpMethods.IsPut(method))
        {
            try
            {
                var set = await JsonSerializer.DeserializeAsync<SetResponseRequest>(context.Request.Body, jsonOptions);
                if (set?.Request?.Match is null || set.Response is null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("'request.match' and 'response' are required");
                    return;
                }

                var updated = overrides.Replace(id, set.Name, set.Request, set.Response);
                if (updated is null)
                    context.Response.StatusCode = 404;
                else
                    await context.Response.WriteAsJsonAsync(updated, jsonOptions);
            }
            catch (JsonException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid JSON body");
            }
            return;
        }

        if (!string.IsNullOrEmpty(id) && HttpMethods.IsDelete(method))
        {
            context.Response.StatusCode = overrides.Remove(id) ? 204 : 404;
            return;
        }

        context.Response.StatusCode = 405;
        return;
    }

    if (!path.StartsWithSegments("/httpdump"))
    {
        var body = overrides.NeedsBody() ? await ResponseOverrideStore.ReadBody(context.Request) : "";
        var match = overrides.Find(context.Request, body);
        if (match is not null)
        {
            context.Response.StatusCode = match.Response.StatusCode;
            foreach (var h in match.Response.Headers ?? [])
            {
                if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    context.Response.ContentType = h.Value;
                else
                    context.Response.Headers[h.Key] = h.Value;
            }
            await context.Response.WriteAsync(match.Response.Body ?? "");
            return;
        }
    }

    if (runtimeConfig.IsReverseProxy)
        await reverseProxyHandler.Handle(context);
    else
        await dumpHandler.Handle(context);

    try
    {
        await next();
    }
    catch
    {
        // ignored
    }
});
app.Run();