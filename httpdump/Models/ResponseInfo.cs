namespace HttpDump.Models;

public record ResponseInfo(
    int StatusCode,
    string? ContentType,
    string?[] ContentEncoding,
    string Content);
