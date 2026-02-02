using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Art.Domain.Constants;
using Art.Infra.Data;
using Art.Infra.Framework;

namespace Art.Core.Services.App;

/// <summary>
/// 玩家服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class PlayerService
{
    private readonly ArtDbContext _db;
    private readonly RequestContext _user;

    public PlayerService(ArtDbContext db, RequestContext user)
    {
        _db = db;
        _user = user;
    }


}
