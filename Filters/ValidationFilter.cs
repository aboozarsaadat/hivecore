using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HiveCore.Filters;

/// <summary>
/// Global action filter that short-circuits the pipeline when ModelState is invalid.
/// - For API requests: returns a standardized JSON 400 response.
/// - For MVC requests: re-renders the current view with the invalid ModelState
///   so that validation messages are shown to the user.
/// </summary>
public class ValidationFilter : IActionFilter, IOrderedFilter
{
    /// <summary>Runs before the default filters so we short-circuit early.</summary>
    public int Order => -1000;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        // ============================================================
        // Build a flat dictionary of { propertyName: [errors] }
        // ============================================================
        var errors = context.ModelState
            .Where(kv => kv.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value!.Errors
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? e.Exception?.Message ?? "Invalid value."
                        : e.ErrorMessage)
                    .ToArray());

        // ============================================================
        // Detect API request (path starts with /api or Accept: application/json)
        // ============================================================
        var request = context.HttpContext.Request;
        var isApiRequest =
            request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            request.Headers["Accept"].ToString().Contains(
                "application/json", StringComparison.OrdinalIgnoreCase) ||
            request.Headers["X-Requested-With"].ToString()
                .Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        if (isApiRequest)
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "Validation failed",
                status = 400,
                errors
            });
            return;
        }

        // ============================================================
        // MVC request: re-render the same view with the invalid model.
        // The framework will use the controller's ViewData, which already
        // contains the ModelState we just inspected. No manual
        // ViewDataDictionary construction is needed.
        // ============================================================
        if (context.Controller is Controller controller)
        {
            var actionName = context.RouteData.Values["action"]?.ToString();

            // If for some reason the action name is missing, fall back to 400.
            if (string.IsNullOrWhiteSpace(actionName))
            {
                context.Result = new BadRequestResult();
                return;
            }

            // Return the same view with the controller's existing ViewData
            // (which already contains ModelState + the invalid model).
            context.Result = new ViewResult
            {
                ViewName = actionName,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };

            return;
        }

        // ============================================================
        // Fallback (e.g. the action is not a Controller — rare)
        // ============================================================
        context.Result = new BadRequestObjectResult(new
        {
            error = "Validation failed",
            status = 400,
            errors
        });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No-op. We only short-circuit on the way IN.
    }
}