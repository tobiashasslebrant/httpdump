using HttpDump.Models;

namespace HttpDump.Cache;

public record ResponseCacheItem(string Hash, RequestInfo RequestInfo, ResponseInfo ResponseInfo);