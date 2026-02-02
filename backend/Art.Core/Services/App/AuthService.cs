using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Art.Domain.Constants;
using Art.Domain.Enums;
using Art.Infra.Data;
using Art.Infra.Framework;

namespace Art.Core.Services.App;

/// <summary>
/// 认证服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class AuthService
{
    private readonly ArtDbContext _db;
    private readonly RequestContext _user;

    public AuthService(ArtDbContext db, RequestContext user)
    {
        _db = db;
        _user = user;
    }
}
