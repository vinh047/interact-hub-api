using System.Security.Claims;

namespace InteractHub.Api.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdString, out Guid userId))
        {
            return userId;
        }

        return Guid.Empty; 
    }
}