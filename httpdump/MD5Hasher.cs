using System.Text;
using System.Text.Json;

public class MD5Hasher
{
    public static string Hash<T>(T obj) 
        => Hash(JsonSerializer.Serialize(obj));

    public static string Hash(string str)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(str);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        for (var i = 0; i < hashBytes.Length; i++)
            sb.Append(hashBytes[i].ToString("X2"));
        return sb.ToString();
    }
}