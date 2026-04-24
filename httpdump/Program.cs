using System.Net;
using System.Text.Json;
using HttpDump;
using HttpDump.Cache;
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

app.Use(async (context, next) =>
{
    var path = context.Request.Path;

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
                reverseProxyScheme = runtimeConfig.ReverseProxyScheme
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

                if (!string.IsNullOrEmpty(host) && (port < 1 || port > 65535))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("reverseProxyPort must be between 1 and 65535");
                    return;
                }

                runtimeConfig.Update(string.IsNullOrEmpty(host) ? null : host, port, scheme ?? "http");
                await inMemoryCache.Clear();

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    isReverseProxy = runtimeConfig.IsReverseProxy,
                    reverseProxyHost = runtimeConfig.ReverseProxyHost,
                    reverseProxyPort = runtimeConfig.ReverseProxyPort,
                    reverseProxyScheme = runtimeConfig.ReverseProxyScheme
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