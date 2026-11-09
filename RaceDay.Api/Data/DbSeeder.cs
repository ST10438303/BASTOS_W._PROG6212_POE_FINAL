using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    /// <summary>
    /// Applies pending EF Core migrations and seeds sample data on startup,
    /// mirroring the seed data from the Part 1 SQL script so manual testing
    /// via Swagger has realistic data to work with immediately.
    /// </summary>
    public static class DbSeeder
    {
        public static void SeedDevelopmentData(RaceDayContext context, IPasswordHasher<User> hasher)
        {
            // Use migrations when the project has them; otherwise create the schema from the model
            // so a fresh checkout (for example inside Docker) still starts with a working database.
            if (context.Database.GetMigrations().Any())
            {
                context.Database.Migrate();
            }
            else
            {
                context.Database.EnsureCreated();
            }

            if (context.Users.Any())
            {
                return; // Already seeded.
            }

            var organiser1 = new User { FullName = "Thandiwe Mokoena", Email = "thandiwe.mokoena@raceday.co.za", Role = RoleNames.Organiser, PhoneNumber = "0821234567" };
            organiser1.PasswordHash = hasher.HashPassword(organiser1, "Password1!");

            var organiser2 = new User { FullName = "Johan van der Merwe", Email = "johan.vdm@raceday.co.za", Role = RoleNames.Organiser, PhoneNumber = "0837654321" };
            organiser2.PasswordHash = hasher.HashPassword(organiser2, "Password1!");

            var participant1 = new User { FullName = "Lindiwe Dlamini", Email = "lindiwe.dlamini@gmail.com", Role = RoleNames.Participant, PhoneNumber = "0713456789" };
            participant1.PasswordHash = hasher.HashPassword(participant1, "Password1!");

            var participant2 = new User { FullName = "Sipho Nkosi", Email = "sipho.nkosi@gmail.com", Role = RoleNames.Participant, PhoneNumber = "0729876543" };
            participant2.PasswordHash = hasher.HashPassword(participant2, "Password1!");

            context.Users.AddRange(organiser1, organiser2, participant1, participant2);
            context.SaveChanges();

            // Dates are relative to "today" so the demo data always contains upcoming events
            // (and one finished event with results) whenever the database is first created.
            var today = DateTime.UtcNow.Date;

            var cycleTour = new Event
            {
                OrganiserId = organiser1.UserId,
                EventName = "Cape Town Cycle Tour",
                Description = "Iconic cycling event around the Cape Peninsula.",
                EventDate = today.AddMonths(-1), // already finished - used for the results demo
                Location = "Cape Town, Western Cape",
                RouteInfo = "Starts at Green Point, loops via Chapmans Peak.",
                Distance = 109.00m,
                EventType = EventTypes.Cycle
            };

            var sowetoMarathon = new Event
            {
                OrganiserId = organiser1.UserId,
                EventName = "Soweto Marathon",
                Description = "Road running event through the streets of Soweto.",
                EventDate = today.AddMonths(3),
                Location = "Soweto, Gauteng",
                RouteInfo = "Starts and finishes at FNB Stadium.",
                Distance = 42.20m,
                EventType = EventTypes.Run
            };

            var beachfrontRun = new Event
            {
                OrganiserId = organiser2.UserId,
                EventName = "Durban Beachfront Park Run",
                Description = "Community fun run along the Durban beachfront.",
                EventDate = today.AddDays(21),
                Location = "Durban, KwaZulu-Natal",
                RouteInfo = "Flat out-and-back route along the promenade.",
                Distance = 5.00m,
                EventType = EventTypes.Walk
            };

            var tableMountainHike = new Event
            {
                OrganiserId = organiser2.UserId,
                EventName = "Table Mountain Summit Walk",
                Description = "A guided charity walk up Platteklip Gorge to the top of Table Mountain.",
                EventDate = today.AddMonths(2),
                Location = "Cape Town, Western Cape",
                RouteInfo = "Platteklip Gorge trailhead to the upper cable station.",
                Distance = 3.50m,
                EventType = EventTypes.Walk
            };

            context.Events.AddRange(cycleTour, sowetoMarathon, beachfrontRun, tableMountainHike);
            context.SaveChanges();

            var cat109 = new Category { EventId = cycleTour.EventId, CategoryName = "109km Cycle", DistanceKm = 109.00m, MaxParticipants = 500, EntryFee = 450.00m };
            var cat35 = new Category { EventId = cycleTour.EventId, CategoryName = "35km Cycle", DistanceKm = 35.00m, MaxParticipants = 300, EntryFee = 250.00m };
            var catMarathon = new Category { EventId = sowetoMarathon.EventId, CategoryName = "42.2km Marathon", DistanceKm = 42.20m, MaxParticipants = 400, EntryFee = 300.00m };
            var catHalf = new Category { EventId = sowetoMarathon.EventId, CategoryName = "21.1km Half Marathon", DistanceKm = 21.10m, MaxParticipants = 400, EntryFee = 200.00m };
            var catSenior = new Category { EventId = sowetoMarathon.EventId, CategoryName = "Senior (Over 40)", MinAge = 40, MaxParticipants = 400, EntryFee = 300.00m };
            var cat5km = new Category { EventId = beachfrontRun.EventId, CategoryName = "5km Fun Run", DistanceKm = 5.00m, MaxParticipants = 200, EntryFee = 0.00m };
            var catHike = new Category { EventId = tableMountainHike.EventId, CategoryName = "Open Walk", DistanceKm = 3.50m, MaxParticipants = 150, EntryFee = 50.00m };

            context.Categories.AddRange(cat109, cat35, catMarathon, catHalf, catSenior, cat5km, catHike);
            context.SaveChanges();

            var enrolment1 = new Enrolment { ParticipantId = participant1.UserId, CategoryId = cat109.CategoryId, Status = EnrolmentStatus.Confirmed, RaceNumber = "CT-1001" };
            var enrolment2 = new Enrolment { ParticipantId = participant1.UserId, CategoryId = cat5km.CategoryId, Status = EnrolmentStatus.Pending, RaceNumber = "DB-2001" };
            var enrolment3 = new Enrolment { ParticipantId = participant2.UserId, CategoryId = catMarathon.CategoryId, Status = EnrolmentStatus.Confirmed, RaceNumber = "SW-3001" };
            var enrolment4 = new Enrolment { ParticipantId = participant2.UserId, CategoryId = cat35.CategoryId, Status = EnrolmentStatus.Confirmed, RaceNumber = "CT-1002" };

            context.Enrolments.AddRange(enrolment1, enrolment2, enrolment3, enrolment4);
            context.SaveChanges();

            context.Results.AddRange(
                new Result
                {
                    EnrolmentId = enrolment1.EnrolmentId,
                    CapturedByUserId = organiser1.UserId,
                    FinishTime = new TimeSpan(4, 15, 30),
                    OverallPosition = 120,
                    CategoryPosition = 45,
                    CompletionStatus = CompletionStatuses.Finished
                },
                new Result
                {
                    EnrolmentId = enrolment4.EnrolmentId,
                    CapturedByUserId = organiser1.UserId,
                    FinishTime = new TimeSpan(1, 38, 5),
                    OverallPosition = 87,
                    CategoryPosition = 12,
                    CompletionStatus = CompletionStatuses.Finished
                });

            context.SaveChanges();
        }
    }
}
