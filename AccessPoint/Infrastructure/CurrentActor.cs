using System.Security.Claims;
namespace App.Infrastructure;
public sealed class CurrentActor(IHttpContextAccessor accessor)
{
    public string Id => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new WorkflowException(401, "Sign in to continue.");
    public bool IsStaff => accessor.HttpContext?.User.IsInRole("Staff") == true || IsAdmin;
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") == true;
}
