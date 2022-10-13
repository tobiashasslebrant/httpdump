using HttpDump.Models;

namespace HttpDump.Cache;

public interface IResponseCache
{
    Task<bool> Contains(string hash);
    Task<ResponseInfo> GetResponse(string hash);
    Task Write(ResponseCacheItem item);
    Task<IEnumerable<ResponseCacheItem>> All();
    Task Clear();
}