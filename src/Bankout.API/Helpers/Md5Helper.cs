using System.Security.Cryptography;
using System.Text;

namespace Bankout.API.Helpers;

public static class Md5Helper
{
    public static string Hash(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
