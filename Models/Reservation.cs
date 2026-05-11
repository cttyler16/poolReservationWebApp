namespace PoolReservationWeb.Models;

public class Reservation
{
    public int Id { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string PinHash { get; set; } = string.Empty;
    public string PinSalt { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public string TimeRangeDisplay =>
        $"{FormatTime(StartTime)} \u2013 {FormatTime(EndTime)}";

    public double DurationHours => (EndTime - StartTime).TotalHours;

    public string DurationDisplay => DurationHours switch
    {
        1.0 => "1 hour",
        1.5 => "1.5 hours",
        2.0 => "2 hours",
        _ => $"{DurationHours:F1} hours"
    };

    private static string FormatTime(TimeSpan t) =>
        DateTime.Today.Add(t).ToString("h:mm tt");
}
