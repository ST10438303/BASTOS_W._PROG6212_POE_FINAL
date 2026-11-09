namespace RaceDay.Web.Services
{
    /// <summary>Keys used to store the logged-in user state in the server-side session.</summary>
    public static class SessionKeys
    {
        /// <summary>Value of the API RaceDay.Session cookie, forwarded on every API call.</summary>
        public const string ApiCookie = "ApiCookie";
        public const string UserId = "UserId";
        public const string FullName = "FullName";
        public const string Role = "Role";
        public const string ProfilePictureUrl = "ProfilePictureUrl";
    }

    public static class RoleNames
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }
}
