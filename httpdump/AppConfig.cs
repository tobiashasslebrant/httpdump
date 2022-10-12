namespace HttpDump;

public record AppConfig(IConfiguration Configuration)
{
    public bool IsReverseProxy => !string.IsNullOrEmpty(ReverseProxyHost);
    public string? ReverseProxyHost => Configuration["ReverseProxyHost"];
    public string? PostgresConnectionString => Configuration["PostgresConnectionString"];
    public int ReverseProxyPort => int.TryParse(Configuration["ReverseProxyPort"], out var val) 
        ? val 
        : 80;
};