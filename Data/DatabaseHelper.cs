using Microsoft.Data.Sqlite;
using PoolReservationWeb.Models;

namespace PoolReservationWeb.Data;

public class DatabaseHelper
{
    private readonly string _connectionString;

    public DatabaseHelper(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "reservations.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var createCmd = new SqliteCommand(@"
            CREATE TABLE IF NOT EXISTS Reservations (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                FamilyName       TEXT    NOT NULL,
                ContactName      TEXT    NOT NULL,
                Phone            TEXT    NOT NULL,
                Email            TEXT    NOT NULL,
                Date             TEXT    NOT NULL,
                StartTime        TEXT    NOT NULL,
                EndTime          TEXT    NOT NULL,
                PinHash          TEXT    NOT NULL,
                PinSalt          TEXT    NOT NULL,
                ConfirmationCode TEXT    UNIQUE NOT NULL,
                CreatedAt        TEXT    NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_date ON Reservations(Date);
            CREATE INDEX IF NOT EXISTS idx_code ON Reservations(ConfirmationCode);
            CREATE TABLE IF NOT EXISTS Settings (
                Key   TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
        ", conn);
        createCmd.ExecuteNonQuery();

        using var checkCmd = new SqliteCommand(
            "SELECT COUNT(*) FROM Settings WHERE Key = 'admin_hash'", conn);
        var alreadySeeded = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

        if (!alreadySeeded)
        {
            var salt = SecurityHelper.GenerateSalt();
            var hash = SecurityHelper.HashPin("admin1234", salt);

            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var (key, val) in new[]
                {
                    ("admin_hash", hash),
                    ("admin_salt", salt),
                    ("pool_open",  "07:00"),
                    ("pool_close", "21:00")
                })
                {
                    using var ins = new SqliteCommand(
                        "INSERT OR IGNORE INTO Settings (Key, Value) VALUES (@K, @V)", conn, tx);
                    ins.Parameters.AddWithValue("@K", key);
                    ins.Parameters.AddWithValue("@V", val);
                    ins.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // Seed new settings for existing installations — no-op if already present.
        var accessSalt = SecurityHelper.GenerateSalt();
        var accessHash = SecurityHelper.HashPin("pool1234", accessSalt);
        using var newDefaults = new SqliteCommand(@"
            INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('access_hash', @AH);
            INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('access_salt', @AS);
            INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('max_duration_hours', '2.0');
        ", conn);
        newDefaults.Parameters.AddWithValue("@AH", accessHash);
        newDefaults.Parameters.AddWithValue("@AS", accessSalt);
        newDefaults.ExecuteNonQuery();
    }

    public string GetSetting(string key, string defaultValue = "")
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("SELECT Value FROM Settings WHERE Key = @K", conn);
        cmd.Parameters.AddWithValue("@K", key);
        return cmd.ExecuteScalar() is string s ? s : defaultValue;
    }

    public (TimeSpan Open, TimeSpan Close) GetPoolHours()
    {
        var open  = GetSetting("pool_open",  "07:00");
        var close = GetSetting("pool_close", "21:00");
        return (TimeSpan.Parse(open), TimeSpan.Parse(close));
    }

    public void SavePoolHours(TimeSpan open, TimeSpan close)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var (key, val) in new[]
            {
                ("pool_open",  open .ToString(@"hh\:mm")),
                ("pool_close", close.ToString(@"hh\:mm"))
            })
            {
                using var cmd = new SqliteCommand(
                    "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@K, @V)", conn, tx);
                cmd.Parameters.AddWithValue("@K", key);
                cmd.Parameters.AddWithValue("@V", val);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void SaveAdminCredentials(string hash, string salt)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var (key, val) in new[] { ("admin_hash", hash), ("admin_salt", salt) })
            {
                using var cmd = new SqliteCommand(
                    "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@K, @V)", conn, tx);
                cmd.Parameters.AddWithValue("@K", key);
                cmd.Parameters.AddWithValue("@V", val);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public double GetMaxDurationHours() =>
        double.TryParse(GetSetting("max_duration_hours", "2.0"),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 2.0;

    public void SaveMaxDurationHours(double hours)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@K, @V)", conn);
        cmd.Parameters.AddWithValue("@K", "max_duration_hours");
        cmd.Parameters.AddWithValue("@V", hours.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public void SaveAccessCredentials(string hash, string salt)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var (key, val) in new[] { ("access_hash", hash), ("access_salt", salt) })
            {
                using var cmd = new SqliteCommand(
                    "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@K, @V)", conn, tx);
                cmd.Parameters.AddWithValue("@K", key);
                cmd.Parameters.AddWithValue("@V", val);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public List<Reservation> GetReservationsForDate(DateTime date)
    {
        var list = new List<Reservation>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT * FROM Reservations WHERE Date = @Date ORDER BY StartTime", conn);
        cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(Map(reader));
        return list;
    }

    public List<Reservation> GetReservationsForRange(DateTime from, DateTime to)
    {
        var list = new List<Reservation>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT * FROM Reservations WHERE Date >= @From AND Date <= @To ORDER BY Date, StartTime", conn);
        cmd.Parameters.AddWithValue("@From", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@To",   to.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(Map(reader));
        return list;
    }

    public bool HasConflict(DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeId = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var sql = @"
            SELECT COUNT(*) FROM Reservations
            WHERE Date = @Date
              AND StartTime < @End
              AND EndTime   > @Start";
        if (excludeId.HasValue) sql += " AND Id != @ExId";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Date",  date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@Start", startTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@End",   endTime.ToString(@"hh\:mm\:ss"));
        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@ExId", excludeId.Value);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public Reservation CreateReservation(Reservation r)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            INSERT INTO Reservations
                (FamilyName, ContactName, Phone, Email, Date, StartTime, EndTime,
                 PinHash, PinSalt, ConfirmationCode, CreatedAt)
            VALUES
                (@FamilyName, @ContactName, @Phone, @Email, @Date, @Start, @End,
                 @PinHash, @PinSalt, @Code, @CreatedAt);
            SELECT last_insert_rowid();", conn);

        cmd.Parameters.AddWithValue("@FamilyName",  r.FamilyName);
        cmd.Parameters.AddWithValue("@ContactName", r.ContactName);
        cmd.Parameters.AddWithValue("@Phone",       r.Phone);
        cmd.Parameters.AddWithValue("@Email",       r.Email);
        cmd.Parameters.AddWithValue("@Date",        r.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@Start",       r.StartTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@End",         r.EndTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@PinHash",     r.PinHash);
        cmd.Parameters.AddWithValue("@PinSalt",     r.PinSalt);
        cmd.Parameters.AddWithValue("@Code",        r.ConfirmationCode);
        cmd.Parameters.AddWithValue("@CreatedAt",   r.CreatedAt.ToString("O"));

        r.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return r;
    }

    public Reservation? GetByConfirmationCode(string code)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT * FROM Reservations WHERE ConfirmationCode = @Code", conn);
        cmd.Parameters.AddWithValue("@Code", code.ToUpper().Trim());
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public bool DeleteReservation(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("DELETE FROM Reservations WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static Reservation Map(SqliteDataReader r) => new()
    {
        Id               = r.GetInt32(r.GetOrdinal("Id")),
        FamilyName       = r.GetString(r.GetOrdinal("FamilyName")),
        ContactName      = r.GetString(r.GetOrdinal("ContactName")),
        Phone            = r.GetString(r.GetOrdinal("Phone")),
        Email            = r.GetString(r.GetOrdinal("Email")),
        Date             = DateTime.Parse(r.GetString(r.GetOrdinal("Date"))),
        StartTime        = TimeSpan.Parse(r.GetString(r.GetOrdinal("StartTime"))),
        EndTime          = TimeSpan.Parse(r.GetString(r.GetOrdinal("EndTime"))),
        PinHash          = r.GetString(r.GetOrdinal("PinHash")),
        PinSalt          = r.GetString(r.GetOrdinal("PinSalt")),
        ConfirmationCode = r.GetString(r.GetOrdinal("ConfirmationCode")),
        CreatedAt        = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt")))
    };
}
