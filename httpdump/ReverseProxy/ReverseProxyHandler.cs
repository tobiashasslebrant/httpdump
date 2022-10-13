using HttpDump.Cache;
using HttpDump.Models;

namespace HttpDump.ReverseProxy;

public record ReverseProxyHandler(
    ReverseProxy ReverseProxy,
    ResponseWriter ResponseWriter,
    ResponseInfoFactory ResponseInfoFactory,
    ResponseCacheFactory CacheFactory)
{
    public async Task Handle(HttpContext context)
    {
        var cache = CacheFactory.Create();

        if (context.Request.Path.Equals("/httpdump/cache"))
            await Cache();
        else if (context.Request.Path.Equals("/httpdump/clear"))
            await Clear();
        else
            await Write();

        async Task Cache()
        {
            var cacheItems = await cache.All();
            await context.Response.WriteAsJsonAsync(cacheItems);
        }

        async Task Clear()
        {
            var count = (await cache.All()).Count();
            await cache.Clear();
            await context.Response.WriteAsync($"{count} cacheItems cleared");
        }
    
        async Task Write()
        {
            var requestInfoFactory = new RequestInfoFactory();
            var requestInfo = await requestInfoFactory.Create(context.Request);
            var hash = MD5Hasher.Hash(requestInfo);

            ResponseInfo responseInfo;
            if (await cache.Contains(hash))
                responseInfo = await cache.GetResponse(hash);
            else
            {
                var httpResponseMessage = await ReverseProxy.Send(context.Request, context.Response);
                responseInfo = await ResponseInfoFactory.Create(httpResponseMessage);
                await cache.Write(new ResponseCacheItem(hash, requestInfo, responseInfo));
            }

            await ResponseWriter.WriteAsync(responseInfo, context.Response);
        }
    }
}