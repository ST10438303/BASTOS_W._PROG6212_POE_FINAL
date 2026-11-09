/* ============================================================
   RaceDay Database Script
   PROG6212 - Portfolio of Evidence
   Target: SQL Server (SSMS)

   Updated for Part 3: Users.ProfilePictureUrl and Events.BannerImageUrl store the URL of
   an image kept in Azure Blob Storage, and Enrolments.Status is now
   Pending / Confirmed / Cancelled (new enrolments start as Pending).

   Updated for Part 2: Events now also capture Distance and EventType
   (Run/Walk/Cycle), and Categories now also support MinAge/MaxAge for
   age-based categories, as required by Part 2's functional requirements.
   This script is generated from (and must match) the EF Core Code-First
   model in RaceDay.Api/Models and RaceDay.Api/Data/RaceDayContext.cs.
   ============================================================ */

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* ============================================================
   TABLE: Users
   ============================================================ */
CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            NVARCHAR(20)    NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    PhoneNumber     NVARCHAR(20)    NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,
    DateCreated     DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* ============================================================
   TABLE: Events
   ============================================================ */
CREATE TABLE Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT             NOT NULL,
    EventName       NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    EventDate       DATE            NOT NULL,
    Location        NVARCHAR(200)   NOT NULL,
    RouteInfo       NVARCHAR(1000)  NULL,
    Distance        DECIMAL(6,2)    NOT NULL,
    EventType       NVARCHAR(10)    NOT NULL CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    BannerImageUrl  NVARCHAR(500)   NULL,
    DateCreated     DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES Users(UserId)
);
GO

/* ============================================================
   TABLE: Categories
   ============================================================ */
CREATE TABLE Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT             NOT NULL,
    CategoryName    NVARCHAR(50)    NOT NULL,
    DistanceKm      DECIMAL(5,2)    NULL,
    MinAge          INT             NULL,
    MaxAge          INT             NULL,
    MaxParticipants INT             NOT NULL DEFAULT 0,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId)
);
GO

/* ============================================================
   TABLE: Enrolments
   ============================================================ */
CREATE TABLE Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT             NOT NULL,
    CategoryId      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    RaceNumber      NVARCHAR(20)    NULL,
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId)
);
GO

/* ============================================================
   TABLE: Results
   ============================================================ */
CREATE TABLE Results (
    ResultId            INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId         INT             NOT NULL UNIQUE,
    CapturedByUserId    INT             NOT NULL,
    FinishTime          TIME            NULL,
    OverallPosition     INT             NULL,
    CategoryPosition    INT             NULL,
    CompletionStatus    NVARCHAR(20)    NOT NULL DEFAULT 'Finished' CHECK (CompletionStatus IN ('Finished', 'DNF', 'DQ')),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CONSTRAINT FK_Results_CapturedBy FOREIGN KEY (CapturedByUserId) REFERENCES Users(UserId)
);
GO

/* ============================================================
   TABLE: Payments
   Retained from Part 1 for schema continuity; no endpoints were
   planned for it, so Part 2 has no PaymentsController.
   ============================================================ */
CREATE TABLE Payments (
    PaymentId       INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT             NOT NULL UNIQUE,
    Amount          DECIMAL(8,2)    NOT NULL,
    PaymentDate     DATETIME        NOT NULL DEFAULT GETDATE(),
    PaymentMethod   NVARCHAR(30)    NOT NULL,
    PaymentStatus   NVARCHAR(20)    NOT NULL DEFAULT 'Pending' CHECK (PaymentStatus IN ('Paid', 'Pending', 'Failed')),
    CONSTRAINT FK_Payments_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId)
);
GO


/* ============================================================
   SEED DATA - matches RaceDay.Api/Data/DbSeeder.cs
   ============================================================ */

INSERT INTO Users (FullName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Thandiwe Mokoena', 'thandiwe.mokoena@raceday.co.za', 'HASHED_PASSWORD_1', 'Organiser', '0821234567'),
('Johan van der Merwe', 'johan.vdm@raceday.co.za', 'HASHED_PASSWORD_2', 'Organiser', '0837654321'),
('Lindiwe Dlamini', 'lindiwe.dlamini@gmail.com', 'HASHED_PASSWORD_3', 'Participant', '0713456789'),
('Sipho Nkosi', 'sipho.nkosi@gmail.com', 'HASHED_PASSWORD_4', 'Participant', '0729876543');

-- Dates are relative to today so the demo data always has upcoming events.
INSERT INTO Events (OrganiserId, EventName, Description, EventDate, Location, RouteInfo, Distance, EventType) VALUES
(1, 'Cape Town Cycle Tour', 'Iconic cycling event around the Cape Peninsula.', DATEADD(MONTH, -1, CAST(GETDATE() AS DATE)), 'Cape Town, Western Cape', 'Starts at Green Point, loops via Chapmans Peak.', 109.00, 'Cycle'),
(1, 'Soweto Marathon', 'Road running event through the streets of Soweto.', DATEADD(MONTH, 3, CAST(GETDATE() AS DATE)), 'Soweto, Gauteng', 'Starts and finishes at FNB Stadium.', 42.20, 'Run'),
(2, 'Durban Beachfront Park Run', 'Community fun run along the Durban beachfront.', DATEADD(DAY, 21, CAST(GETDATE() AS DATE)), 'Durban, KwaZulu-Natal', 'Flat out-and-back route along the promenade.', 5.00, 'Walk'),
(2, 'Table Mountain Summit Walk', 'A guided charity walk up Platteklip Gorge to the top of Table Mountain.', DATEADD(MONTH, 2, CAST(GETDATE() AS DATE)), 'Cape Town, Western Cape', 'Platteklip Gorge trailhead to the upper cable station.', 3.50, 'Walk');

INSERT INTO Categories (EventId, CategoryName, DistanceKm, MinAge, MaxAge, MaxParticipants, EntryFee) VALUES
(1, '109km Cycle', 109.00, NULL, NULL, 500, 450.00),
(1, '35km Cycle', 35.00, NULL, NULL, 300, 250.00),
(2, '42.2km Marathon', 42.20, NULL, NULL, 400, 300.00),
(2, '21.1km Half Marathon', 21.10, NULL, NULL, 400, 200.00),
(2, 'Senior (Over 40)', NULL, 40, NULL, 400, 300.00),
(3, '5km Fun Run', 5.00, NULL, NULL, 200, 0.00),
(4, 'Open Walk', 3.50, NULL, NULL, 150, 50.00);

INSERT INTO Enrolments (ParticipantId, CategoryId, Status, RaceNumber) VALUES
(3, 1, 'Confirmed', 'CT-1001'),
(3, 6, 'Pending',   'DB-2001'),
(4, 3, 'Confirmed', 'SW-3001'),
(4, 2, 'Confirmed', 'CT-1002');

INSERT INTO Results (EnrolmentId, CapturedByUserId, FinishTime, OverallPosition, CategoryPosition, CompletionStatus) VALUES
(1, 1, '04:15:30', 120, 45, 'Finished'),
(4, 1, '01:38:05', 87, 12, 'Finished');
GO
