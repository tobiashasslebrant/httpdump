using HttpDump.Extensions;
using HttpDump.Models;

namespace HttpDump;

public class RequestInfoFactory
{
    public async Task<RequestInfo> Create(HttpRequest request)
    {
        using var sr = new StreamReader(await request.Body.DeepCopy());
        return new RequestInfo(
            request.Method,
            request.Protocol,
            request.Path.Value,
            request.QueryString.Value,
            request.Headers.Keys.Select(key => new
            {
                key, 
                values=request.Headers[key].ToArray()
            }).ToDictionary(k => k.key, s => s.values),
            await sr.ReadToEndAsync());
    }
}