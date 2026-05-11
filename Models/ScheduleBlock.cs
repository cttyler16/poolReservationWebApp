namespace PoolReservationWeb.Models;

public class ScheduleBlock
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsReserved { get; set; }
    public Reservation? Reservation { get; set; }

    public string StartTimeDisplay => DateTime.Today.Add(StartTime).ToString("h:mm tt");
    public string EndTimeDisplay   => DateTime.Today.Add(EndTime).ToString("h:mm tt");
    public string TimeRangeDisplay => $"{StartTimeDisplay} \u2013 {EndTimeDisplay}";

    public double DurationHours => (EndTime - StartTime).TotalHours;

    public string DurationDisplay
    {
        get
        {
            var h = DurationHours;
            if (h == Math.Floor(h))
                return $"{(int)h} hr{(h == 1.0 ? "" : "s")}";
            return $"{h:F1} hrs";
        }
    }
}
