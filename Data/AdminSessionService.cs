namespace PoolReservationWeb.Data;

/// <summary>
/// Scoped per Blazor circuit — tracks admin login state for the current browser session.
/// </summary>
public class AdminSessionService
{
    public bool IsLoggedIn { get; private set; }
    public void Login()  => IsLoggedIn = true;
    public void Logout() => IsLoggedIn = false;
}
