# RaceDay – Part 3: MVC Web Application, Azure Blob Storage & Docker

RaceDay is a full-stack event management platform for the South African road running, walking and cycling community.
This repository holds all three parts of the PROG6212 Portfolio of Evidence. **Part 3** adds:

* an **ASP.NET Core MVC** web front end (`RaceDay.Web`) that consumes the Part 2 REST API,
* **session-based authentication** with a distinct interface for each role (Organiser / Participant),
* **Azure Blob Storage** image uploads (event banners and profile pictures), handled **by the API**,
* **Docker** containerisation, plus a `docker-compose.yml` that starts the entire platform with one command,
* an updated **GitHub Actions** workflow that builds the API *and* the MVC project, runs the tests and builds the Docker images.

## Architecture

```
 Browser ──► RaceDay.Web (MVC, :5200) ──HTTP + session cookie──► RaceDay.Api (REST, :5100) ──► SQL Server
                                                                         │
                                                                         └──► Azure Blob Storage (images)
```

* The **MVC project contains no database access, no business rules and no Azure code.** It only calls the API
  (`Services/ApiClient.cs`). It has no project reference to `RaceDay.Api`, and no EF Core or Azure packages.
* **All file handling goes through the API.** The browser uploads an image to the MVC app, the MVC app forwards it to the
  API (`POST /api/events/{id}/banner`, `POST /api/users/me/picture`), and the API validates it and stores it in Blob Storage.
* **Session management.** On login the MVC app calls `POST /api/auth/login`, keeps the API's session cookie in the user's
  server-side session (`ISession`), and forwards it on every API call. The user's id, name, role and profile picture are kept
  in the same session, and an MVC authentication cookie drives `[Authorize(Roles = ...)]`. The login cookie is only valid while
  its matching session exists; if the API ends the session (HTTP 401) the user is signed out and sent to the login page.

## Roles and pages

| | Organiser | Participant |
|---|---|---|
| **Home** | Dashboard: events created, enrolments per event, upcoming dates | Upcoming events with search / type / date filters |
| **Events** | Manage events: create, edit, delete (name, description, date, location, distance, type, **banner image**) | Event detail page: banner, full information, categories |
| **Categories** | Add / edit / delete age or distance categories | View categories and spots left |
| **Enrolments** | See every participant, their category and status; **confirm** pending enrolments | Enrol in a category; "My enrolments" with status; cancel |
| **Results** | Capture finish times and positions (hh:mm:ss); saved results are visible to participants immediately | "My results": event, date, category, finish time and position (e.g. *47th of 312 finishers*) |
| **Profile** | View / update details, upload profile picture | View / update details, upload profile picture |

Navigation menus differ per role. Unauthenticated users are redirected to the login page for protected pages; a user of the
wrong role gets a friendly "access denied" page. Enrolment statuses are colour coded: **green = Confirmed**,
**amber = Pending**, grey = Cancelled.

## Running it

### Option A – everything in Docker (one command)

Requires Docker Desktop.

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| MVC web app | http://localhost:5200 |
| REST API + Swagger | http://localhost:5100/swagger |
| Azurite (Azure Blob emulator) | http://localhost:10000 |
| SQL Server | `localhost,1433` (user `sa`, password in `docker-compose.yml`) |

The first start takes a few minutes (SQL Server must come up before the API starts). The API creates the database and seeds demo data automatically.

### Option B – only the MVC app in Docker (single `docker run` command)

The API must already be running on the host (see Option C, steps 1-4). Then:

```bash
docker build -t raceday-web .
docker run --rm -p 5200:8080 --add-host=host.docker.internal:host-gateway -e Api__BaseUrl=http://host.docker.internal:5100 raceday-web
```

Open http://localhost:5200. The container behaves exactly like the app running locally – only the API address differs (`Api__BaseUrl`).

### Option C – run locally without Docker

Prerequisites: .NET 8 SDK, SQL Server (LocalDB is fine), and an Azure Blob Storage account **or** the free Azurite emulator.

1. **Blob Storage.** Either start Azurite:
   ```bash
   docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck --loose
   ```
   (the default `appsettings.json` already points at it with `UseDevelopmentStorage=true`), **or** use a real Azure Storage account:
   ```bash
   dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your storage connection string>" --project RaceDay.Api
   ```
   The container (`raceday-images`) is created automatically with public read access for blobs.
   For a real account, *Allow Blob anonymous access* must be enabled on the storage account.
2. **Database.** Part 3 adds columns (`Users.ProfilePictureUrl`, `Events.BannerImageUrl`) and changes enrolment statuses, so create a migration:
   ```bash
   dotnet tool install --global dotnet-ef        # once
   dotnet ef migrations add Part3_ImagesAndStatuses --project RaceDay.Api
   dotnet ef database update --project RaceDay.Api
   ```
   (If you have no earlier migration, the same commands create the whole schema. In `Development` the API also applies migrations
   and seeds demo data on start-up.)
3. **Start the API** (plain http on port 5100 – the MVC app expects this address):
   ```bash
   dotnet run --project RaceDay.Api --launch-profile http
   ```
4. **Start the MVC app** (port 5200):
   ```bash
   dotnet run --project RaceDay.Web
   ```
   The API address is set in `RaceDay.Web/appsettings.json` (`Api:BaseUrl`).
5. Browse to http://localhost:5200.

### Demo accounts (seeded in Development)

| Email | Password | Role |
|---|---|---|
| thandiwe.mokoena@raceday.co.za | Password1! | Organiser |
| johan.vdm@raceday.co.za | Password1! | Organiser |
| lindiwe.dlamini@gmail.com | Password1! | Participant |
| sipho.nkosi@gmail.com | Password1! | Participant |

## Tests and CI/CD

```bash
dotnet test
```

The API test suite (xUnit, in-memory database, Azure replaced by a fake storage service) covers registration/login, event
management, role rejection, enrolments and the Pending → Confirmed flow, results (including finisher totals), event filtering,
the organiser's `/mine` endpoint, and banner / profile-picture uploads (valid, wrong role, non-owner, non-image file).

`.github/workflows/ci.yml` runs on every push: it restores, builds the **API project, the MVC project and the whole solution**,
runs all tests, then builds both Docker images (`Dockerfile`, `Dockerfile.api`) and validates `docker-compose.yml`.

**CI/CD green build screenshot:** *[insert screenshot of a successful GitHub Actions run here before submission]*

## Design decisions and changes to the Part 2 API

Part 3 needed a few API additions. All are documented in `docs/API_Endpoint_Plan.md`, `docs/RaceDay_ERD.png` and `docs/RaceDay_Database.sql`.

* **Blob Storage lives in the API** (`Services/BlobStorageService.cs` behind `IBlobStorageService`). Uploads are validated (JPG / PNG / WEBP,
  max 5 MB, and the file's first bytes are checked so a renamed non-image is rejected). Only the resulting URL is stored in the database.
* **Enrolment status** is now `Pending` (default) → `Confirmed` (by the event's Organiser via `PUT /api/enrolments/{id}/confirm`) or `Cancelled`.
  This replaces Part 2's `Active`, to support the green *Confirmed* / amber *Pending* colour coding required by the UI.
* **New fields:** `Users.ProfilePictureUrl`, `Events.BannerImageUrl`.
* **New / extended endpoints:** `GET /api/events` filters (`search`, `eventType`, `upcomingOnly`); `GET /api/events/mine` (dashboard data with enrolment counts);
  results now return `eventDate` and `totalFinishers` so the UI can show "47th of 312 finishers".
* **Safer deletes:** an event or category that has enrolments returns `409 Conflict` instead of failing in the database.
* **"Publish" results:** saving a result makes it visible to the participant straight away, so there is no separate publish step.
* **One fixed culture** (`InvariantCulture`) is used by the MVC app so numbers and dates behave identically locally and in Docker.
* **Error handling:** API failures are translated into short messages; if the API is unreachable the page still renders with a friendly notice.
  Unhandled errors show a styled error page with a reference id, never a stack trace (outside Development).

## Video presentation

An unlisted YouTube video demonstrates the full running application, explains the MVC structure, Blob Storage integration and Docker setup (no AI-generated voice).

**YouTube video link:** *[insert unlisted YouTube link here before submission]*

## Repository layout

```
RaceDay.sln
Dockerfile                    MVC app image          docker-compose.yml   full stack (SQL Server, Azurite, API, MVC)
Dockerfile.api                API image              .github/workflows/ci.yml
RaceDay.Api/                  REST API (Part 2 + Blob Storage)
RaceDay.Api.Tests/            xUnit tests
RaceDay.Web/                  ASP.NET Core MVC front end (Part 3)
  Controllers/  Services/ (ApiClient, session filter)  Models/ (API contracts, view models)  Views/  wwwroot/
docs/                         ERD, endpoint plan, SQL script
```

## Submission checklist

- [ ] `docker compose up --build` starts everything; MVC works at http://localhost:5200
- [ ] `docker build` + `docker run` command above starts the MVC container against a running API
- [ ] Banner and profile picture uploads appear on the pages (stored in Blob Storage)
- [ ] `dotnet test` passes; GitHub Actions is green (API + MVC build)
- [ ] 20+ meaningful commits on the Part 3 branch
- [ ] README updated with CI screenshot and YouTube link
- [ ] GitHub link submitted on ARC
