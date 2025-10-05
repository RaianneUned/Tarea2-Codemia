using System.Linq;
using Microsoft.AspNetCore.Http;
using Tarea2.Models;
using Tarea2;

namespace Tarea2.Services;

public interface IUserSessionService
{
    bool IsAuthenticated(HttpContext context);
    string? GetCurrentUsername(HttpContext context);
    string? GetCurrentDisplayName(HttpContext context);
    void SignIn(HttpContext context, UserRecord user);
    void SignOut(HttpContext context);
}

public class UserSessionService : IUserSessionService
{
    private const string UsernameKey = "CurrentUser:Username";
    private const string DisplayNameKey = "CurrentUser:DisplayName";

    public bool IsAuthenticated(HttpContext context)
    {
        return context.Session.GetString(UsernameKey) != null;
    }

    public string? GetCurrentUsername(HttpContext context)
    {
        return context.Session.GetString(UsernameKey);
    }

    public string? GetCurrentDisplayName(HttpContext context)
    {
        return context.Session.GetString(DisplayNameKey);
    }

    public void SignIn(HttpContext context, UserRecord user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
            ? user.Username
            : string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        context.Session.SetString(UsernameKey, user.Username);
        context.Session.SetString(DisplayNameKey, displayName);
    }

    public void SignOut(HttpContext context)
    {
        context.Session.Remove(UsernameKey);
        context.Session.Remove(DisplayNameKey);
        context.Session.Remove(SessionConstants.RecentProjects);
    }
}
