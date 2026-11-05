using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;

namespace HttpDump;

/// <summary>
/// HTTP basic auth for the /httpdump* administrative routes. Enabled only when both
/// HTTPDUMP_USER and HTTPDUMP_PWD are set (non-empty).
/// </summary>
public class AdminAuth
{
    private readonly byte[]? _userHash;
    private readonly byte[]? _pwdHash;

    public AdminAuth(IConfiguration configuration)
    {
        var user = configuration["HTTPDUMP_USER"];
        var pwd = configuration["HTTPDUMP_PWD"];
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pwd)) return;
        _userHash = Hash(user);
        _pwdHash = Hash(pwd);
    }

    public bool Enabled => _userHash is not null;

    /// <summary>True if the request carries valid credentials (or auth is disabled).</summary>
    public bool IsAuthorized(HttpRequest request)
    {
        if (!Enabled) return true;
        if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var header)
            || !"Basic".Equals(header.Scheme, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(header.Parameter))
            return false;

        string decoded;
        try { decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter)); }
        catch (FormatException) { return false; }

        var separator = decoded.IndexOf(':');
        if (separator < 0) return false;

        // Compare fixed-length hashes in constant time; evaluate both so timing doesn't reveal which was wrong.
        var userOk = CryptographicOperations.FixedTimeEquals(Hash(decoded[..separator]), _userHash!);
        var pwdOk = CryptographicOperations.FixedTimeEquals(Hash(decoded[(separator + 1)..]), _pwdHash!);
        return userOk & pwdOk;
    }

    public static void Challenge(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.Headers.WWWAuthenticate = "Basic realm=\"httpdump\", charset=\"UTF-8\"";
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
