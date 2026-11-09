using System.Net;

namespace RaceDay.Web.Services
{
    /// <summary>Marker type for API calls that return no body.</summary>
    public sealed class EmptyResponse { }

    /// <summary>
    /// The outcome of an API call. Controllers never see raw HTTP errors - failures are
    /// translated into a short, user-friendly message in Error.
    /// </summary>
    public class ApiResult<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public HttpStatusCode StatusCode { get; init; }
        public string? Error { get; init; }

        public static ApiResult<T> Ok(T? data, HttpStatusCode status = HttpStatusCode.OK) =>
            new() { Success = true, Data = data, StatusCode = status };

        public static ApiResult<T> Fail(HttpStatusCode status, string error) =>
            new() { Success = false, StatusCode = status, Error = error };

        public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
        public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
    }

    /// <summary>Thrown when the API says the session is no longer valid (HTTP 401) on a protected call.</summary>
    public class ApiSessionExpiredException : Exception
    {
        public ApiSessionExpiredException() : base("The API session has expired.") { }
    }
}
