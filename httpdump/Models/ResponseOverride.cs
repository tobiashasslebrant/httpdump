namespace HttpDump.Models;

public record ResponseOverride(
    string Id,
    string Name,
    OverrideRequest Request,
    OverrideResponse Response);

public record OverrideRequest(OverrideMatch Match);

public record OverrideMatch
{
    public string? Method { get; init; }
    public string? Protocol { get; init; }
    public string? BaseUrl { get; init; }
    public string? RelativeUrl { get; init; }
    public List<OverrideQueryParameter>? QueryParameters { get; init; }
    public bool IgnoreQueryParameters { get; init; }
    public List<OverrideHeader>? Headers { get; init; }
    public string? Body { get; init; }
}

public record OverrideQueryParameter(string Key, string? Value);

public record OverrideResponse
{
    public int StatusCode { get; init; } = 200;
    public List<OverrideHeader>? Headers { get; init; }
    public string? Body { get; init; }
}

public record OverrideHeader(string Key, string Value);

public record SetResponseRequest(string? Name, OverrideRequest? Request, OverrideResponse? Response);
