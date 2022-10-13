using System.Text.Json;
using HttpDump.Models;

namespace HttpDump;

public record DumpHandler(RequestInfoFactory RequestInfoFactory)
{
    private readonly List<RequestCache> _cache = new();

    public async Task Handle(HttpContext context)
    {
        if (context.Request.Path.Equals("/httpdump/cache"))
            await Cache();
        else if (context.Request.Path.Equals("/httpdump/clear"))
            await Clear();
        else
            await Write();

        async Task Write()
        {
            var requestInfo = await RequestInfoFactory.Create(context.Request);
            var hash = MD5Hasher.Hash(requestInfo);
            var cacheItem = _cache.FirstOrDefault(f => f.Hash == hash);
            if (cacheItem is null)
                _cache.Add(new RequestCache(hash, 1, requestInfo));
            else
                cacheItem.Count++;

            await context.Response.WriteAsync("saved to cache");
        }

        async Task Cache()
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(_cache, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        async Task Clear()
        {
            var count = _cache.Count;
            _cache.Clear();
            await context.Response.WriteAsync($"Removed {count} items from cache");
        }
    }
    
    record RequestCache(string Hash, int Count, RequestInfo Item)
    {
        public int Count { get; set; } = Count;
    }
}