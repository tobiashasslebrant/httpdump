namespace HttpDump;

public class RuntimeConfig
{
    private string? _reverseProxyHost;
    private int _reverseProxyPort;
    private readonly object _lock = new();

    public RuntimeConfig(IConfiguration configuration)
    {
        _reverseProxyHost = configuration["ReverseProxyHost"];
        _reverseProxyPort = int.TryParse(configuration["ReverseProxyPort"], out var val) ? val : 80;
    }

    public string? ReverseProxyHost
    {
        get { lock (_lock) return _reverseProxyHost; }
    }

    public int ReverseProxyPort
    {
        get { lock (_lock) return _reverseProxyPort; }
    }

    public bool IsReverseProxy
    {
        get { lock (_lock) return !string.IsNullOrEmpty(_reverseProxyHost); }
    }

    public void Update(string? host, int port)
    {
        lock (_lock)
        {
            _reverseProxyHost = host;
            _reverseProxyPort = port;
        }
    }
}
