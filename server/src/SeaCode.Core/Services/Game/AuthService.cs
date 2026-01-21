using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using SeaCode.Domain.Constants;
using SeaCode.Domain.Enums;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;

namespace SeaCode.Core.Services.Game;

/// <summary>
/// 认证服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class AuthService
{
    private readonly GameDbContext _db;
    private readonly RequestContext _user;

    public AuthService(GameDbContext db, RequestContext user)
    {
        _db = db;
        _user = user;
    }
}
