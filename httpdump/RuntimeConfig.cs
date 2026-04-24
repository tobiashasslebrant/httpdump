namespace HttpDump;

public class RuntimeConfig
{
    private string? _reverseProxyHost;
    private int _reverseProxyPort;
    private string _reverseProxyScheme;
    private readonly object _lock = new();

    public RuntimeConfig(IConfiguration configuration)
    {
        _reverseProxyHost = configuration["ReverseProxyHost"];
        _reverseProxyPort = int.TryParse(configuration["ReverseProxyPort"], out var val) ? val : 80;
        _reverseProxyScheme = configuration["ReverseProxyScheme"]?.ToLowerInvariant() == "https" ? "https" : "http";
    }

    public string? ReverseProxyHost
    {
        get { lock (_lock) return _reverseProxyHost; }
    }

    public int ReverseProxyPort
    {
        get { lock (_lock) return _reverseProxyPort; }
    }

    public string ReverseProxyScheme
    {
        get { lock (_lock) return _reverseProxyScheme; }
    }

    public bool IsReverseProxy
    {
        get { lock (_lock) return !string.IsNullOrEmpty(_reverseProxyHost); }
    }

    public void Update(string? host, int port, string scheme)
    {
        lock (_lock)
        {
            _reverseProxyHost = host;
            _reverseProxyPort = port;
            _reverseProxyScheme = scheme == "https" ? "https" : "http";
        }
    }
}
