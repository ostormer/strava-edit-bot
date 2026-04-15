# StravaAPILibrary — Agent Guide

Strava API reference: https://developers.strava.com/docs/reference/

A .NET 10 class library wrapping the Strava REST API v3. Provides static API helper classes and model types. No external package dependencies — uses only BCL (`System.Net.Http`, `System.Text.Json`).

---

## Project structure

```
API/
  Activities.cs         # Get/update activities, laps, zones, comments, kudos; upload files
  Athletes.cs           # Athlete profile endpoints
  Clubs.cs              # Club endpoints
  Gears.cs              # Gear endpoints
  Routes.cs             # Route endpoints
  Segments.cs           # Segment search and details
  SegmentsEfforts.cs    # Segment effort endpoints
  Streams.cs            # Activity stream data
  Uploads.cs            # Upload status polling
Authentication/
  UserAuthentication.cs # OAuth 2.0 flow: browser redirect, local callback listener, token exchange/refresh
  credentials.cs        # Credentials class: ClientId, ClientSecret, Scope, AccessToken, RefreshToken, TokenExpiration
Models/
  Activities/           # DetailedActivity, SummaryActivity, Lap, Split, MetaActivity, UpdatableActivity
  Athletes/             # DetailedAthlete, SummaryAthlete, MetaAthlete
  Clubs/                # DetailedClub, SummaryClub, ClubActivity, ClubAthlete, MetaClub
  Gears/                # DetailedGear, SummaryGear
  Routes/               # Route, Waypoint
  Segments/             # DetailedSegment, SummarySegment, DetailedSegmentEffort, SummarySegmentEffort, SummaryPRSegmentEffort
  Streams/              # BaseStream + typed streams (altitude, cadence, distance, heartrate, latlng, moving, power, grade, velocity, temperature, time)
  Enums/                # ActivityType, SportType
  Common/               # LatLng
  Explorer/             # ExplorerResponse, ExplorerSegment
  Maps/                 # PolylineMap
  Photos/               # PhotosSummary, PhotosSummaryPrimary
  Uploads/              # Upload
  ActivityStats.cs, ActivityTotal.cs, ActivityZone.cs, Comment.cs,
  Error.cs, Fault.cs, StreamSet.cs, TimedZoneDistribution.cs,
  TimedZoneRange.cs, Zones/
```

---

## API classes

All classes in `API/` are **static** and stateless. Every method takes an `accessToken` as its first parameter and returns raw `JsonObject` or `JsonArray` from `System.Text.Json.Nodes`. Each method creates and disposes its own `HttpClient` — there is no shared client.

Key methods on `Activities`:

| Method | HTTP | Description |
|---|---|---|
| `GetAthletesActivitiesAsync` | GET | List activities with optional date filter + pagination |
| `GetActivityByIdAsync` | GET | Single activity by ID |
| `GetActivityLapsAsync` | GET | Laps for an activity |
| `GetActivityZonesAsync` | GET | HR/power zones for an activity |
| `GetActivityCommentsAsync` | GET | Comments on an activity |
| `GetActivityKudosAsync` | GET | Kudos for an activity |
| `PostActivityAsync` | POST | Upload a fit/gpx/tcx file |
| `UpdateActivityAsync` | PUT | Update name, description, sport type, gear, trainer/commute flags |

---

## Authentication

`UserAuthentication` implements the full OAuth 2.0 authorization code flow for desktop/CLI use:

1. `StartAuthorization()` — opens the system browser to `https://www.strava.com/oauth/authorize`
2. `WaitForAuthCodeAsync()` — spins up an `HttpListener` on the redirect URI port, waits up to 5 min for the callback
3. `ExchangeCodeForTokenAsync(code)` — POSTs to `https://www.strava.com/oauth/token` and stores tokens in `Credentials`
4. `RefreshAccessTokenAsync()` — uses the refresh token to get a new access token

`Credentials` holds `ClientId`, `ClientSecret`, `Scope`, `AccessToken`, `RefreshToken`, and `TokenExpiration` (`DateTime`). Access tokens expire after 6 hours; refresh tokens do not expire unless revoked.
