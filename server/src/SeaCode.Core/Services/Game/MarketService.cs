using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RedisClient = FreeRedis.RedisClient;
using Microsoft.Extensions.DependencyInjection;
using SeaCode.Domain.Constants;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;

namespace SeaCode.Core.Services.Game;

/// <summary>
/// 市场服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class MarketService
{
    private readonly GameDbContext _db;
    private readonly RequestContext _user;
    private readonly RedisClient _cache;

    public MarketService(GameDbContext db, RequestContext user, RedisClient cache)
    {
        _db = db;
        _user = user;
        _cache = cache;
    }
}
