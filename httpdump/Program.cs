using System.Net;
using HttpDump;
using HttpDump.Cache;
using HttpDump.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<DumpHandler>();

builder.Services.AddTransient<ReverseProxyHandler>();
builder.Services.AddTransient<ReverseProxy>();

builder.Services.AddTransient<ResponseCacheFactory>();
builder.Services.AddTransient<ResponseWriter>();

builder.Services.AddTransient<ResponseInfoFactory>();
builder.Services.AddTransient<RequestInfoFactory>();

builder.Services.AddTransient<AppConfig>();
builder.Services.AddTransient<PostgresCache>();
builder.Services.AddSingleton<InMemoryResponseCache>();

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