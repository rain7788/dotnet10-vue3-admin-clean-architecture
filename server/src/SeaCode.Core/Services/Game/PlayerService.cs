using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeaCode.Domain.Constants;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;

namespace SeaCode.Core.Services.Game;

/// <summary>
/// 玩家服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class PlayerService
{
    private readonly GameDbContext _db;
    private readonly RequestContext _user;

    public PlayerService(GameDbContext db, RequestContext user)
    {
        _db = db;
        _user = user;
    }


}
