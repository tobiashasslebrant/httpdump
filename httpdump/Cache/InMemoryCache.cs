using HttpDump.Models;

namespace HttpDump.Cache;

public record InMemoryResponseCache : IResponseCache
{
    private readonly List<ResponseCacheItem> _cacheItems = new();

    public async Task<bool> Contains(string hash) 
        => await Task.FromResult(_cacheItems.Any(a => a.Hash == hash));

    public async Task<ResponseInfo> GetResponse(string hash)
        => await Task.FromResult(_cacheItems.First(a => a.Hash == hash).ResponseInfo);

    public async Task Write(ResponseCacheItem item)
    {
        _cacheItems.Add(item);
        await Task.CompletedTask;
    }   

    public async Task<IEnumerable<ResponseCacheItem>> All()
        => await Task.FromResult(_cacheItems.AsEnumerable());

    public async Task Clear()
    {
        _cacheItems.Clear();
        await Task.CompletedTask;
    }
}