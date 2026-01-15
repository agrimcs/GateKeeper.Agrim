using GateKeeper.Application.Common;
using GateKeeper.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace GateKeeper.Server.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider services)
    {
        // Resolution order:
        // 1. X-Tenant header (for local/dev convenience)
        // 2. tenant query parameter (for local/dev convenience)
        // 3. subdomain of the host (production)

        string? tenantIdentifier = null;

        // 1) X-Tenant header
        if (context.Request.Headers.TryGetValue("X-Tenant", out var headerValues))
        {
            tenantIdentifier = headerValues.FirstOrDefault();
        }

        // 2) tenant query parameter
        if (tenantIdentifier == null && context.Request.Query.TryGetValue("tenant", out var q))
        {
            tenantIdentifier = q.FirstOrDefault();
        }

        // 3) subdomain
        if (tenantIdentifier == null)
        {
            var host = context.Request.Host.Host; // e.g., tenant.example.com or tenant.localhost
            var segments = host.Split('.');
            if (segments.Length > 2)
            {
                tenantIdentifier = segments[0];
            }
        }

        if (!string.IsNullOrEmpty(tenantIdentifier))
        {
            // Resolve tenant repository from DI scope and find organization by subdomain or id
            var scopeFactory = services.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
            if (scopeFactory != null)
            {
                using var scope = scopeFactory.CreateScope();
                var orgRepo = scope.ServiceProvider.GetService(typeof(GateKeeper.Domain.Interfaces.IOrganizationRepository)) as GateKeeper.Domain.Interfaces.IOrganizationRepository;
                if (orgRepo != null)
                {
                    GateKeeper.Domain.Entities.Organization? org = null;

                    // Try treat identifier as GUID first
                    if (Guid.TryParse(tenantIdentifier, out var guid))
                    {
                        org = await orgRepo.GetByIdAsync(guid);
                    }

                    // Otherwise treat as subdomain
                    if (org == null)
                    {
                        org = await orgRepo.GetBySubdomainAsync(tenantIdentifier);
                    }

                    if (org != null && org.IsActive)
                    {
                        context.Items["TenantId"] = org.Id;
                        context.Items["Tenant"] = org;
                    }
                }
            }
        }

        await _next(context);
    }
}
