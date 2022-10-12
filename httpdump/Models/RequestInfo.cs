namespace HttpDump.Models;

public record RequestInfo(
    string RequestMethod, 
    string RequestProtocol,
    string? RequestPath,
    string? QueryStringValue,
    Dictionary<string, string[]> Headers,
    string Body);