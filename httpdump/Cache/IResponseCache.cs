using HttpDump.Models;

namespace HttpDump.Cache;

public interface IResponseCache
{
    Task<bool> Contains(string hash);
    Task<ResponseInfo> Get(string hash);
    Task Write(string hash, RequestInfo request, ResponseInfo response);
    Task<IEnumerable<ResponseCacheItem>> All();
    Task Clear();
}