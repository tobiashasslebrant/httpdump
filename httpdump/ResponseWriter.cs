using System.Text;
using HttpDump.Models;

namespace HttpDump;

public record ResponseWriter
{
    public async Task WriteAsync(ResponseInfo response, HttpResponse proxyResponse)
    {
        proxyResponse.StatusCode = (response.StatusCode);

        if (response.ContentType != null)
            proxyResponse.ContentType = response.ContentType;

        if(response.ContentEncoding.Any())
            proxyResponse.Headers.TryAdd("Content-Encoding", string.Join(',', response.ContentEncoding));

        var bytes = Encoding.UTF8.GetBytes(response.Content);
            
        await using var stream = new MemoryStream(bytes);
        await stream.CopyToAsync(proxyResponse.Body);
    }
}