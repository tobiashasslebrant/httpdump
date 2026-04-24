namespace HttpDump;

public record AppConfig(IConfiguration Configuration, RuntimeConfig RuntimeConfig)
{
    public bool IsReverseProxy => RuntimeConfig.IsReverseProxy;
    public string? ReverseProxyHost => RuntimeConfig.ReverseProxyHost;
    public string? PostgresConnectionString => Configuration["PostgresConnectionString"];
    public int ReverseProxyPort => RuntimeConfig.ReverseProxyPort;
};