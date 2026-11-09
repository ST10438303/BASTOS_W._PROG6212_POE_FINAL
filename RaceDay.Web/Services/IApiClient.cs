using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services
{
    /// <summary>
    /// The only way the MVC application reaches data: HTTP calls to the Part 2 API.
    /// The API session cookie is stored in the user's MVC session and forwarded automatically.
    /// </summary>
    public interface IApiClient
    {
        Task<ApiResult<T>> GetAsync<T>(string url);
        Task<ApiResult<T>> PostAsync<T>(string url, object? body = null);
        Task<ApiResult<T>> PutAsync<T>(string url, object? body = null);
        Task<ApiResult<EmptyResponse>> DeleteAsync(string url);

        /// <summary>Uploads a file as multipart/form-data (field name "file").</summary>
        Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file);

        /// <summary>Logs in against the API and keeps the API session cookie in the MVC session.</summary>
        Task<ApiResult<AuthResponse>> LoginAsync(string email, string password);

        /// <summary>Ends the API session (best effort) and forgets the stored API cookie.</summary>
        Task LogoutAsync();
    }
}
