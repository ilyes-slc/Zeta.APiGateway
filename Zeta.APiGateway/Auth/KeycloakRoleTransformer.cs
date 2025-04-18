using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Zeta.APiGateway.Auth
{
    public class KeycloakRoleTransformer : IClaimsTransformation
    {
        private readonly ILogger<KeycloakRoleTransformer> _logger;

        public KeycloakRoleTransformer(ILogger<KeycloakRoleTransformer> logger)
        {
            _logger = logger;
        }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;

            if (claimsIdentity?.IsAuthenticated != true)
            {
                return Task.FromResult(principal);
            }

            try
            {
                // Process realm_access roles
                ProcessRealmRoles(claimsIdentity);
                
                // Process resource_access roles if needed
                ProcessResourceRoles(claimsIdentity);

                // Log the transformed claims for debugging
                _logger.LogDebug("Transformed claims: {Claims}", 
                    string.Join(", ", claimsIdentity.Claims.Select(c => $"{c.Type}: {c.Value}")));
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw to avoid authentication failures
                _logger.LogError(ex, "Error transforming Keycloak roles");
            }

            return Task.FromResult(principal);
        }

        private void ProcessRealmRoles(ClaimsIdentity claimsIdentity)
        {
            var realmAccessClaim = claimsIdentity.FindFirst("realm_access");
            if (realmAccessClaim == null)
            {
                _logger.LogWarning("No realm_access claim found in the token");
                return;
            }

            try
            {
                var realmAccessJson = JsonDocument.Parse(realmAccessClaim.Value);
                if (realmAccessJson.RootElement.TryGetProperty("roles", out var rolesElement) && 
                    rolesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName))
                        {
                            // Add role as a separate claim with our custom type for Ocelot authorization
                            claimsIdentity.AddClaim(new Claim("realm_access_roles", roleName));
                            // Also add as a standard role claim
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                            
                            _logger.LogDebug("Added realm role: {Role}", roleName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing realm roles from claim value: {ClaimValue}", realmAccessClaim.Value);
            }
        }

        private void ProcessResourceRoles(ClaimsIdentity claimsIdentity)
        {
            var resourceAccessClaim = claimsIdentity.FindFirst("resource_access");
            if (resourceAccessClaim == null)
            {
                return;
            }

            try
            {
                var resourceAccessJson = JsonDocument.Parse(resourceAccessClaim.Value);
                foreach (var resource in resourceAccessJson.RootElement.EnumerateObject())
                {
                    var resourceName = resource.Name;
                    if (resource.Value.TryGetProperty("roles", out var rolesElement) && 
                        rolesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in rolesElement.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            if (!string.IsNullOrEmpty(roleName))
                            {
                                // Add resource-specific role
                                var resourceRoleClaimType = $"resource_{resourceName}_roles";
                                claimsIdentity.AddClaim(new Claim(resourceRoleClaimType, roleName));
                                
                                _logger.LogDebug("Added resource role: {Resource}:{Role}", resourceName, roleName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing resource roles from claim value: {ClaimValue}", resourceAccessClaim.Value);
            }
        }
    }
}
