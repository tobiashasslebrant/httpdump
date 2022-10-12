using HttpDump.Models;

namespace HttpDump;

public class ResponseInfoFactory
{
    public async Task<ResponseInfo> Create(HttpResponseMessage responseMessage) 
        => new(
            (int) responseMessage.StatusCode,
            responseMessage.Content.Headers.ContentType?.MediaType,
            responseMessage.Content.Headers.ContentEncoding.ToArray(),
            await responseMessage.Content.ReadAsStringAsync());
}