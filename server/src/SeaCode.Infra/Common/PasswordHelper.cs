using System.Security.Cryptography;
using System.Text;

namespace SeaCode.Infra.Common;

/// <summary>
/// 密码辅助类
/// 提供密码加密相关功能，供 Admin 和 Game 服务共用
/// </summary>
public static class PasswordHelper
{
    /// <summary>
    /// 计算密码哈希（SHA256 + Base64）
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>加密后的密码</returns>
    public static string ComputeHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hash">存储的哈希值</param>
    /// <returns>是否匹配</returns>
    public static bool Verify(string password, string hash)
    {
        return ComputeHash(password) == hash;
    }

    /// <summary>
    /// 生成随机密码
    /// </summary>
    /// <param name="length">密码长度</param>
    /// <returns>随机密码</returns>
    public static string GenerateRandom(int length = 12)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
