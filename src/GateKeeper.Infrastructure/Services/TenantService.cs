using GateKeeper.Application.Common;
using GateKeeper.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GateKeeper.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentTenantId()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return null;

        // 1. Check if middleware set it via HttpContext.Items (from subdomain/header/query)
        if (ctx.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
            return tenantId;

        // 2. Check JWT claims for "org" claim (set during login)
        var orgClaim = ctx.User?.FindFirst("org");
        if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var orgId))
            return orgId;

        return null;
    }

    public async Task<Organization?> GetCurrentTenantAsync()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return null;
        if (ctx.Items.TryGetValue("Tenant", out var tenantObj) && tenantObj is Organization org)
            return org;
        return null;
    }

    public bool HasTenantContext()
    {
        return GetCurrentTenantId() != null;
    }
}