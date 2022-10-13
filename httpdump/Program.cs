using System.Net;
using HttpDump;
using HttpDump.Cache;
using HttpDump.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<DumpHandler>()
    .AddTransient<ReverseProxyHandler>()
    .AddTransient<ReverseProxy>()
    .AddTransient<ResponseCacheFactory>()
    .AddTransient<ResponseWriter>()
    .AddTransient<ResponseInfoFactory>()
    .AddTransient<RequestInfoFactory>()
    .AddTransient<AppConfig>()
    .AddTransient<PostgresCache>()
    .AddSingleton<InMemoryResponseCache>();

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHttpClient("HttpClientWithSSLUntrusted").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });

builder.Services.AddControllers();

var app = builder.Build();

var config = app.Services.GetService<AppConfig>();
var reverseProxyHandler = app.Services.GetService<ReverseProxyHandler>();
var dumpHandler = app.Services.GetService<DumpHandler>();

app.Use(async (context, next) =>
{
    if (config.IsReverseProxy)
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