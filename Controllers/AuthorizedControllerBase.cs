using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Shared base for all authenticated controllers.
/// Provides a single helper to extract the logged-in user's ID from the JWT.
/// </summary>
public abstract class AuthorizedControllerBase : ControllerBase
{
    /// <summary>
    /// Returns the authenticated user's ID parsed from the JWT "sub" claim,
    /// or null if the token is missing / malformed.
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue("sub");

        return int.TryParse(claim, out var id) ? id : null;
    }
}

