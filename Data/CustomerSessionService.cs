namespace PoolReservationWeb.Data;

/// <summary>
/// Scoped per Blazor circuit — tracks whether the current visitor has entered
/// the customer access code and is allowed to make reservations.
/// </summary>
public class CustomerSessionService
{
    public bool IsAuthenticated { get; private set; }
    public void Authenticate() => IsAuthenticated = true;
    public void Clear()        => IsAuthenticated = false;
}
