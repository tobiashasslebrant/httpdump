using System.Text;
using System.Text.RegularExpressions;
using HttpDump.Models;

namespace HttpDump;

/// <summary>In-memory list of response overrides. Newest matching override wins.</summary>
public class ResponseOverrideStore
{
    private readonly List<ResponseOverride> _items = new();
    private readonly object _lock = new();

    public ResponseOverride Add(string? name, OverrideRequest request, OverrideResponse response)
    {
        var match = request.Match;
        var item = new ResponseOverride(
            Guid.NewGuid().ToString("N"),
            string.IsNullOrWhiteSpace(name) ? DefaultName(match) : name,
            request,
            response);
        lock (_lock) _items.Add(item);
        return item;
    }

    public ResponseOverride? Replace(string id, string? name, OverrideRequest request, OverrideResponse response)
    {
        lock (_lock)
        {
            var index = _items.FindIndex(i => i.Id == id);
            if (index < 0) return null;
            var item = new ResponseOverride(
                id,
                string.IsNullOrWhiteSpace(name) ? DefaultName(request.Match) : name,
                request,
                response);
            _items[index] = item;
            return item;
        }
    }

    public bool Remove(string id)
    {
        lock (_lock) return _items.RemoveAll(i => i.Id == id) > 0;
    }

    public IReadOnlyList<ResponseOverride> All()
    {
        lock (_lock) return _items.ToList();
    }

    public bool NeedsBody()
    {
        lock (_lock) return _items.Any(i => !string.IsNullOrEmpty(i.Request.Match.Body));
    }

    public ResponseOverride? Find(HttpRequest request, string body)
    {
        List<ResponseOverride> snapshot;
        lock (_lock) snapshot = _items.ToList();
        for (var i = snapshot.Count - 1; i >= 0; i--)
            if (Matches(snapshot[i].Request.Match, request, body))
                return snapshot[i];
        return null;
    }

    private static string DefaultName(OverrideMatch m)
        => $"{m.Protocol}{m.BaseUrl}/{m.RelativeUrl?.TrimStart('/')}"
           + (m.QueryParameters is { Count: > 0 }
               ? "?" + string.Join('&', m.QueryParameters.Select(q => $"{q.Key}={q.Value}"))
               : "");

    private static bool Matches(OverrideMatch m, HttpRequest req, string body)
    {
        if (!Wildcard(m.Method, req.Method, ignoreCase: true)) return false;
        if (!Wildcard(m.Protocol?.TrimEnd(':', '/'), req.Scheme, ignoreCase: true)) return false;

        if (!string.IsNullOrEmpty(m.BaseUrl)
            && !Wildcard(m.BaseUrl, req.Host.Value ?? "", true)
            && !Wildcard(m.BaseUrl, req.Host.Host ?? "", true))
            return false;

        var path = (req.PathBase + req.Path).Value?.TrimStart('/') ?? "";
        if (!Wildcard(m.RelativeUrl?.TrimStart('/'), path, ignoreCase: false)) return false;

        if (!m.IgnoreQueryParameters && m.QueryParameters is { Count: > 0 })
        {
            foreach (var q in m.QueryParameters.Where(q => !string.IsNullOrEmpty(q.Key)))
            {
                if (!req.Query.TryGetValue(q.Key, out var values)
                    || !values.Any(v => Wildcard(q.Value, v ?? "", false)))
                    return false;
            }
        }

        // Header names are case-insensitive; at least one value of the header must match the pattern.
        foreach (var h in (m.Headers ?? []).Where(h => !string.IsNullOrEmpty(h.Key)))
        {
            if (!req.Headers.TryGetValue(h.Key, out var values)
                || !values.Any(v => Wildcard(h.Value, v ?? "", false)))
                return false;
        }

        return Wildcard(m.Body, body, ignoreCase: false);
    }

    /// <summary>Null/empty pattern matches anything; '*' matches any characters.</summary>
    private static bool Wildcard(string? pattern, string input, bool ignoreCase)
    {
        if (string.IsNullOrEmpty(pattern)) return true;
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var options = RegexOptions.Singleline | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        return Regex.IsMatch(input, regex, options);
    }

    public static async Task<string> ReadBody(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }
}
