namespace HttpDump.Extensions;

public static class StreamExtensions
{
    public static async Task<Stream> DeepCopy(this Stream stream)
    {
        var newStream = new MemoryStream();
        await stream.CopyToAsync(newStream);
        return newStream;
    }
}