using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RedisClient = FreeRedis.RedisClient;
using Microsoft.Extensions.DependencyInjection;
using Art.Domain.Constants;
using Art.Infra.Data;
using Art.Infra.Framework;

namespace Art.Core.Services.App;

/// <summary>
/// 市场服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class MarketService
{
    private readonly ArtDbContext _db;
    private readonly RequestContext _user;
    private readonly RedisClient _cache;

    public MarketService(ArtDbContext db, RequestContext user, RedisClient cache)
    {
        _db = db;
        _user = user;
        _cache = cache;
    }
}
