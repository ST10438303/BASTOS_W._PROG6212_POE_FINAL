namespace RaceDay.Web.Models.Api
{
    // Request bodies sent to the API. Property names serialise to camelCase.

    public record RegisterRequest(string FullName, string Email, string Password, string? PhoneNumber, string Role);

    public record UpdateProfileRequest(string FullName, string? PhoneNumber);

    public record EventRequest(
        string EventName, string? Description, DateTime EventDate, string Location,
        string? RouteInfo, decimal Distance, string EventType);

    public record CategoryRequest(
        string CategoryName, decimal? DistanceKm, int? MinAge, int? MaxAge, int MaxParticipants, decimal EntryFee);

    public record EnrolmentRequest(int CategoryId);

    public record ResultRequest(
        int EnrolmentId, TimeSpan? FinishTime, int? OverallPosition, int? CategoryPosition, string CompletionStatus);
}
