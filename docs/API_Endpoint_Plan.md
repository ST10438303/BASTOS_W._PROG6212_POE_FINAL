# RaceDay – API Endpoint Plan

This document lists every API endpoint the RaceDay system exposes, as planned in Part 1 and implemented in Part 2. It covers Authentication, User Profile, Events, Categories, Event Enrolments, and Results.

> **Part 3 additions:** `GET /api/events` now accepts `search`, `eventType` and `upcomingOnly` filters; new endpoints `GET /api/events/mine`, `POST /api/events/{id}/banner`, `POST /api/users/me/picture` and `PUT /api/enrolments/{id}/confirm`; results now include `eventDate` and `totalFinishers`; enrolment statuses are `Pending` (default), `Confirmed` and `Cancelled`; events and categories that have enrolments can no longer be deleted (409). Images are uploaded to the API, which stores them in Azure Blob Storage - the MVC app never talks to Azure directly.
>
> **Note on deviations (Part 2):** Events now also capture `Distance` and `EventType` (Run/Walk/Cycle), and Categories now support age-based entries via `MinAge`/`MaxAge` in addition to `DistanceKm`. These were required by Part 2's functional requirements but were not in the original Part 1 ERD. See `README.md` for the full explanation. The endpoints below have been updated accordingly; no endpoint routes changed.

Roles: **None** = public/no login required · **Any** = any logged-in user · **Organiser** / **Participant** = restricted to that role.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either a Participant or an Organiser. | None (public) | `{ fullName, email, password, phoneNumber, role }` | 201 Created. 400 Bad Request – validation failed / invalid role. 409 Conflict – email already registered. |
| POST | /api/auth/login | Authenticates a user and issues a session cookie. | None (public) | `{ email, password }` | 200 OK – session cookie issued. 401 Unauthorized – invalid credentials. |
| POST | /api/auth/logout | Ends the current session. | Any | None | 200 OK. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Retrieves the logged-in user's own profile. | Any | None | 200 OK. 401 Unauthorized. |
| PUT | /api/users/me | Updates the logged-in user's own profile. | Any | `{ fullName, phoneNumber }` | 200 OK. 400 Bad Request. 401 Unauthorized. |
| POST | /api/users/me/picture | Uploads the profile picture to Azure Blob Storage. | Any | multipart/form-data, field `file` (JPG/PNG/WEBP, max 5 MB) | 200 OK – profile with `profilePictureUrl`. 400 (not an image / too large). 401. 503 (storage unavailable). |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists events, optionally filtered by `?search=`, `?eventType=Run\|Walk\|Cycle`, `?upcomingOnly=true`. | None (public) | None | 200 OK. 400 (invalid eventType). |
| GET | /api/events/mine | Lists the logged-in Organiser's events with enrolment counts (dashboard). | Organiser | None | 200 OK. 403. |
| GET | /api/events/{id} | Retrieves a single event. | None (public) | None | 200 OK. 404 Not Found. |
| POST | /api/events | Creates a new event (name, description, date, location, distance, event type). | Organiser | `{ eventName, description, eventDate, location, routeInfo, distance, eventType }` | 201 Created. 400 Bad Request. 401/403. |
| PUT | /api/events/{id} | Updates an event owned by the logged-in Organiser. | Organiser (owner) | same as create | 200 OK. 400/403/404. |
| POST | /api/events/{id}/banner | Uploads the event banner image to Azure Blob Storage. | Organiser (owner) | multipart/form-data, field `file` (JPG/PNG/WEBP, max 5 MB) | 200 OK – `{ bannerImageUrl }`. 400. 403/404. 503. |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser (owner) | None | 200 OK. 403/404. 409 (event has enrolments). |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists categories for an event. | None (public) | None | 200 OK. 404 Not Found. |
| POST | /api/events/{eventId}/categories | Adds an age- or distance-based category. | Organiser (owner) | `{ categoryName, distanceKm, minAge, maxAge, maxParticipants, entryFee }` | 201 Created. 403/404. |
| PUT | /api/categories/{id} | Updates a category. | Organiser (owner) | same as create | 200 OK. 403/404. |
| DELETE | /api/categories/{id} | Removes a category. | Organiser (owner) | None | 200 OK. 403/404. 409 (category has enrolments). |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments | Enrols the logged-in Participant into a category. New enrolments start as **Pending**. | Participant | `{ categoryId }` | 201 Created – includes generated race number. 400 Bad Request (full/invalid). 403. 409 Conflict (already enrolled). |
| GET | /api/enrolments/mine | Lists the logged-in Participant's own enrolments. | Participant | None | 200 OK. |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event. | Organiser (owner) | None | 200 OK. 403/404. |
| PUT | /api/enrolments/{id}/confirm | Confirms a Pending enrolment (Pending → Confirmed). | Organiser (event owner) | None | 200 OK. 400 (cancelled). 403/404. |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant (owner) | None | 200 OK. 403/404. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/results | Captures a finishing result for an enrolment. | Organiser (event owner) | `{ enrolmentId, finishTime, overallPosition, categoryPosition, completionStatus }` | 201 Created. 400/403/404. 409 Conflict (already captured). |
| GET | /api/results/mine | Retrieves the logged-in Participant's own results, including `eventDate` and `totalFinishers` (e.g. "47th of 312 finishers"). | Participant | None | 200 OK. |
| GET | /api/events/{eventId}/results | Public leaderboard for an event. | None (public) | None | 200 OK. 404 Not Found. |
| PUT | /api/results/{id} | Corrects a previously captured result. | Organiser (event owner) | same as create | 200 OK. 400/403/404. |

---
*Originally produced for Part 1 of the RaceDay POE (PROG6212); updated in Part 2 to reflect the implemented API.*
