using HttpDump.Models;
using Npgsql;

namespace HttpDump.Cache;

public record PostgresCache(AppConfig Config) : IResponseCache
{
    private async Task Initialize()
    {
        await using var connection = new NpgsqlConnection(Config.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($@"
            create table if not exists tbl_httpdump_responseInfo(Hash varchar, StatusCode int, ContentType varchar, ContentEncoding varchar, Content varchar);
            create table if not exists tbl_httpdump_requestInfo(Hash varchar, RequestMethod varchar, RequestProtocol varchar, RequestPath varchar, QueryStringValue varchar, Headers varchar, Body varchar);"
            , connection);
        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<bool> Contains(string hash)
    {
        await Initialize();
        await using var connection = new NpgsqlConnection(Config.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($@"
            select count (*) 
            from tbl_httpdump_responseinfo 
            where Hash = '{hash}'"
            , connection);
        var value = (long?)(await command.ExecuteScalarAsync());
        return value > 0;
    }

    public async Task<ResponseInfo> Get(string hash)
    {
        
        await using var connection = new NpgsqlConnection(Config.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($@"
            select StatusCode, ContentType, ContentEncoding, Content 
            from tbl_httpdump_responseinfo 
            where Hash = '{hash}'"
            , connection);
        var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return MapResponse(reader);
    }
    
    public async Task Write(string hash, RequestInfo request, ResponseInfo response)
    {
        await Initialize();
        var headers = string.Join(';', request.Headers.Select(s => $"{s.Key}:{string.Join(',', s.Value)}"));
        await using var connection = new NpgsqlConnection(Config.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($@"
            insert into tbl_httpdump_responseInfo(Hash, StatusCode, ContentType, ContentEncoding, Content) 
            values ('{hash}', {response.StatusCode}, '{response.ContentType}', '{string.Join(',',response.ContentEncoding)}', '{response.Content}');

            insert into tbl_httpdump_requestInfo(Hash, RequestMethod, RequestProtocol, RequestPath, QueryStringValue, Headers, Body)
            values('{hash}', '{request.RequestMethod}', '{request.RequestProtocol}', '{request.RequestPath}', '{request.QueryStringValue}', '{headers}', '{request.Body}');"
            , connection);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<ResponseCacheItem>> All()
    {
        await Initialize();
        await using var connection = new NpgsqlConnection(Config.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($@"
            select Hash, StatusCode, ContentType, ContentEncoding, Content 
            from tbl_httpdump_responseinfo"
            , connection);
        await using var reader = await command.ExecuteReaderAsync();

        var responseInfos = new Dictionary<string,ResponseInfo>();
        while (await reader.ReadAsync())
            responseInfos.Add((string)reader["hash"], MapResponse(reader));   
        
        await using var connection2 = new NpgsqlConnection(Config.PostgresConnectionString);
        await connection2.OpenAsync();
        await using var command2 = new NpgsqlCommand($@"
            select Hash, RequestMethod, RequestProtocol, RequestPath, QueryStringValue, Headers, Body
            from tbl_httpdump_requestInfo"
            , connection2);
        await using var reader2 = await command2.ExecuteReaderAsync();

        var requestInfos = new Dictionary<string, RequestInfo>();
        while (await reader2.ReadAsync())
            requestInfos.Add((string)reader2["hash"],MapRequest(reader2));

        return responseInfos.OrderBy(o => o.Key)
            .Zip(requestInfos.OrderBy(o => o.Key))
            .Select(s => new ResponseCacheItem(s.First.Key, s.Second.Value, s.First.Value));
    }

    public async Task Clear()
    {
        await Initialize();
        await using var connection = new NpgsqlConnection(Config.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($@"
            delete from tbl_httpdump_responseInfo; 
            delete from tbl_httpdump_requestInfo;"
            , connection);

        await command.ExecuteNonQueryAsync();
    }
    
    ResponseInfo MapResponse(NpgsqlDataReader reader)
        => new(
            (int)reader["StatusCode"], 
            (string)reader["ContentType"],
            ((string)reader["ContentEncoding"]).Split(','),
            (string)reader["Content"]);
    
    RequestInfo MapRequest(NpgsqlDataReader reader)
    {
        var headers = ((string) reader["Headers"]).Split(';')
            .Select(s => (s.Split(':').First(), s.Split(':').Last().Split(',')))
            .ToDictionary(s => s.Item1, ss => ss.Item2);
            
        return new(
            (string) reader["RequestMethod"],
            (string) reader["RequestProtocol"],
            (string) reader["RequestPath"],
            (string) reader["QueryStringValue"],
            headers,
            (string) reader["Body"]);
    }
}