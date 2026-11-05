namespace HttpDump.Extensions;

public static class StreamExtensions
{
    /// <summary>
    /// Copies the stream into a new MemoryStream positioned at 0. If the source is seekable
    /// (see HttpRequest.EnableBuffering) it is rewound before and after, so it can be copied again.
    /// </summary>
    public static async Task<Stream> DeepCopy(this Stream stream)
    {
        if (stream.CanSeek) stream.Position = 0;
        var newStream = new MemoryStream();
        await stream.CopyToAsync(newStream);
        if (stream.CanSeek) stream.Position = 0;
        newStream.Position = 0;
        return newStream;
    }
}
