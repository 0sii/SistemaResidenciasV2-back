namespace WebApiVinculacionProyectosV2.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Formato: "PERM:Proyecto-Update"
        if (policyName.StartsWith("PERM:", StringComparison.OrdinalIgnoreCase))
        {
            var perm = policyName.Substring("PERM:".Length);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(perm))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}

