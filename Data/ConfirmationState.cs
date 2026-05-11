using PoolReservationWeb.Models;

namespace PoolReservationWeb.Data;

/// <summary>
/// Scoped per Blazor circuit — holds the just-created reservation + plain-text PIN
/// so the Confirmation page can display them once after booking.
/// </summary>
public class ConfirmationState
{
    public Reservation? Reservation { get; private set; }
    public string? Pin { get; private set; }
    public bool HasData => Reservation is not null;

    public void Set(Reservation r, string pin)
    {
        Reservation = r;
        Pin = pin;
    }

    public (Reservation r, string pin) TakeAndClear()
    {
        var result = (Reservation!, Pin!);
        Reservation = null;
        Pin = null;
        return result;
    }
}
