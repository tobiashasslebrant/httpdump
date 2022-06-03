using System.Text.Json;

namespace httpdump;

public class ResponseWriter
{
    private readonly List<CacheItem> _cache = new();

    public async Task Write(HttpContext context)
    {
        using var sr = new StreamReader(context.Request.Body);
        var requestInfo = new RequestInfo(
            context.Request.Method,
            context.Request.Protocol,
            context.Request.Path.Value,
            context.Request.QueryString.Value,
            context.Request.Headers.Keys.Select(key => new
            {
                key, 
                values=context.Request.Headers[key].ToArray()
            }).ToDictionary(k => k.key, s => s.values),
            await sr.ReadToEndAsync());
        
        var hash = MD5Hasher.Hash(requestInfo);
        var cacheItem = _cache.FirstOrDefault(f => f.Hash == hash);
        if (cacheItem is null)
            _cache.Add(new CacheItem(hash, 1, requestInfo));
        else
            cacheItem.Count++;
        
        await context.Response.WriteAsync("saved to cache");
    }

    public async Task Cache(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(_cache, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    public async Task Clear(HttpContext context)
    {
        var count = _cache.Count;
        _cache.Clear();
        await context.Response.WriteAsync($"Removed {count} items from cache");
    }
    
    record RequestInfo(
        string RequestMethod, 
        string RequestProtocol,
        string? RequestPath,
        string? QueryStringValue,
        Dictionary<string, string[]> Headers,
        string Body);

    record CacheItem(string Hash, int Count, RequestInfo Item)
    {
        public int Count { get; set; } = Count;
    }
}

