namespace HttpDump.Cache;

public record ResponseCacheFactory(IServiceProvider Services, AppConfig AppConfig)
{
    public IResponseCache Create()
        => (string.IsNullOrEmpty(AppConfig.PostgresConnectionString)
            ? Services.GetService<InMemoryResponseCache>()
            : Services.GetService<PostgresCache>())!;

}