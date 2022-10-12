using HttpDump.Extensions;

namespace HttpDump.ReverseProxy;

public record ReverseProxy(IHttpClientFactory HttpClientFactory, AppConfig Config)
{
    public async Task<HttpResponseMessage> Send(HttpRequest request, HttpResponse response)
    {
        var client = HttpClientFactory.CreateClient("HttpClientWithSSLUntrusted");
        var message = await CreateHttpRequestMessage(request);
        var responseMessage = await client.SendAsync(message, CancellationToken.None);
        await WriteAsync(responseMessage, response);
        return responseMessage;
    }
    
    async Task<HttpRequestMessage> CreateHttpRequestMessage(HttpRequest req)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new UriBuilder
            {
                Scheme = req.Scheme,
                Host = Config.ReverseProxyHost,
                Port = Config.ReverseProxyPort,
                Path = req.PathBase.Add(req.Path),
                Query = req.QueryString.ToString()
            }.Uri,
            Method = new HttpMethod(req.Method),
            Content = new StreamContent(await req.Body.DeepCopy())
        };

        foreach (var (key, value) in req.Headers)
            request.Headers.TryAddWithoutValidation(key, value.AsEnumerable());

        if (!string.IsNullOrEmpty(req.ContentType))
            request.Content!.Headers.Add("Content-Type", req.ContentType);
        return request;
    }
    
    private static async Task WriteAsync(HttpResponseMessage msg, HttpResponse response)
    {
        response.StatusCode = (int)msg.StatusCode;

        if (msg.Content.Headers.ContentType?.MediaType != null)
            response.ContentType = msg.Content.Headers.ContentType.MediaType;

        if (msg.Content.Headers.ContentEncoding.Any())
            response.Headers.TryAdd("Content-Encoding", string.Join(',', msg.Content.Headers.ContentEncoding));

        await using var stream = await msg.Content.ReadAsStreamAsync();
        await stream.CopyToAsync(response.Body);
    }
}