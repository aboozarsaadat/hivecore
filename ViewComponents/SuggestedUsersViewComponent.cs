using HiveCore.Data;
using HiveCore.Models.DTOs;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.ViewComponents;

/// <summary>
/// Renders the "Who to follow" widget in the right rail.
/// </summary>
public class SuggestedUsersViewComponent : ViewComponent
{
    private readonly IUserService _userService;

    public SuggestedUsersViewComponent(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        int count = 5,
        CancellationToken ct = default)
    {
        // Guests get nothing here (view returns empty)
        var userIdClaim = UserClaimsPrincipal?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return View(new List<UserSummaryDto>());
        }

        count = Math.Clamp(count, 1, 15);
        var users = await _userService.GetSuggestedAsync(userId, count, ct);
        return View(users.ToList());
    }
}