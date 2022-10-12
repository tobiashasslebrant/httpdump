namespace HttpDump.Cache;

public record ResponseCacheFactory(IServiceProvider Services)
{
    public IResponseCache Create(AppConfig config)
        => (string.IsNullOrEmpty(config.PostgresConnectionString)
            ? Services.GetService<InMemoryResponseCache>()
            : Services.GetService<PostgresCache>())!;

}