using System.Security.Cryptography;
using System.Text;

namespace Art.Core.Shared;

/// <summary>
/// 密码处理工具类
/// </summary>
public static class PasswordHelper
{
    /// <summary>
    /// 计算密码哈希（SHA256 + Base64）
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>哈希后的密码</returns>
    public static string ComputeHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 验证密码是否匹配
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hashedPassword">已哈希的密码</param>
    /// <returns>是否匹配</returns>
    public static bool Verify(string password, string hashedPassword)
    {
        return ComputeHash(password) == hashedPassword;
    }
}
