# 🏊 Pool Reservation Web App

A Blazor Server web application for managing a community pool schedule. Enforces a one-family-at-a-time policy with a maximum of 2 hours per session, and gives residents a simple self-service way to book and cancel time slots.

## Features

- **Weekly calendar view** — see reserved and open blocks across a 3-week window (current week ± 1 week); click any open slot to jump straight to the booking form with the date and time pre-filled
- **Self-service reservations** — book up to 14 days in advance; choose start time and duration (up to the configured max)
- **Conflict detection** — overlapping time slots are blocked in real time and re-checked at submission
- **Confirmation code + PIN** — each reservation gets a unique code and a 6-character PIN for self-service lookup and cancellation
- **Manage page** — residents can look up and cancel their own reservations without contacting an admin
- **Admin panel** — password-protected dashboard to view any day's schedule, delete reservations, adjust pool hours, and change the admin password

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core / Blazor Server (.NET 10) |
| Database | SQLite via `Microsoft.Data.Sqlite` |
| Rendering | Interactive Server render mode |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run locally

```bash
git clone https://github.com/cttyler16/poolReservationWebApp.git
cd poolReservationWebApp
dotnet run
```

The app will be available at `https://localhost:5001` (or the port shown in terminal output).

The SQLite database (`data/reservations.db`) is created automatically on first run.

## Default Admin Credentials

| Field | Value |
|---|---|
| URL | `/admin` |
| Password | `admin1234` |

> ⚠️ **Change the default password immediately after first login** via the Admin → Settings tab.

## Project Structure

```
poolReservationWebApp/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor          # Weekly calendar view
│   │   ├── Reserve.razor       # Booking form
│   │   ├── Confirmation.razor  # Post-booking summary
│   │   ├── Manage.razor        # Look up / cancel a reservation
│   │   ├── Admin.razor         # Admin dashboard
│   │   ├── Error.razor
│   │   └── NotFound.razor
│   └── Layout/
├── Data/
│   ├── DatabaseHelper.cs       # SQLite data access
│   ├── AdminSessionService.cs  # Scoped admin login state
│   ├── ConfirmationState.cs    # Passes confirmation data to the confirmation page
│   └── SecurityHelper.cs       # PIN hashing and confirmation code generation
├── Models/
│   ├── Reservation.cs          # Reservation entity
│   └── ScheduleBlock.cs        # View model for schedule display
├── wwwroot/                    # Static assets (CSS, etc.)
├── Program.cs
└── appsettings.json
```

## How It Works

### Booking a Slot
1. From the home calendar, click **Reserve a Time Slot**.
2. Select a date (today through 14 days out), start time, and duration.
3. The form shows existing reservations for that day and flags conflicts in real time.
4. Fill in family name, contact name, phone, and email, then submit.
5. A **confirmation code** and **PIN** are displayed — save these to manage or cancel later.

### Managing / Cancelling a Reservation
1. Click **Manage My Reservation** from the home page.
2. Enter your confirmation code and PIN.
3. View your reservation details or cancel it.

### Admin Panel (`/admin`)
- **Schedule tab** — browse any date's reservations and delete them if needed.
- **Settings tab** — configure pool opening/closing hours and change the admin password.

## Configuration

Pool hours default to **7:00 AM – 9:00 PM** and are stored in the database. They can be changed at any time from the Admin → Settings tab without restarting the app.
