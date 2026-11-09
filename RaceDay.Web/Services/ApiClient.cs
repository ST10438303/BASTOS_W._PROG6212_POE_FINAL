using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services
{
    public class ApiClient : IApiClient
    {
        private const string ApiCookieName = "RaceDay.Session";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor, ILogger<ApiClient> logger)
        {
            _http = http;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public Task<ApiResult<T>> GetAsync<T>(string url) =>
            SendAsync<T>(HttpMethod.Get, url, null);

        public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
            SendAsync<T>(HttpMethod.Post, url, ToJsonContent(body));

        public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) =>
            SendAsync<T>(HttpMethod.Put, url, ToJsonContent(body));

        public Task<ApiResult<EmptyResponse>> DeleteAsync(string url) =>
            SendAsync<EmptyResponse>(HttpMethod.Delete, url, null);

        public async Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
        {
            var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", Path.GetFileName(file.FileName));

            return await SendAsync<T>(HttpMethod.Post, url, content);
        }

        public async Task<ApiResult<AuthResponse>> LoginAsync(string email, string password)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = ToJsonContent(new { email, password })
            };

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Could not reach the RaceDay API during login.");
                return ApiResult<AuthResponse>.Fail(0, UnreachableMessage);
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return ApiResult<AuthResponse>.Fail(response.StatusCode, "Your email or password is incorrect.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<AuthResponse>.Fail(response.StatusCode, await ReadErrorAsync(response));
                }

                CaptureSessionCookie(response);

                var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
                return body == null
                    ? ApiResult<AuthResponse>.Fail(response.StatusCode, UnexpectedMessage)
                    : ApiResult<AuthResponse>.Ok(body, response.StatusCode);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await SendAsync<EmptyResponse>(HttpMethod.Post, "api/auth/logout", null);
            }
            catch (ApiSessionExpiredException)
            {
                // Already logged out on the API side - nothing more to do.
            }
            finally
            {
                Session.Remove(SessionKeys.ApiCookie);
            }
        }

        // ------------------------------------------------------------------ internals

        private const string UnreachableMessage =
            "We could not reach the RaceDay service. Please try again in a moment.";

        private const string UnexpectedMessage =
            "The RaceDay service sent an unexpected response. Please try again.";

        private static HttpContent? ToJsonContent(object? body) =>
            body == null ? null : JsonContent.Create(body, body.GetType(), null, JsonOptions);

        private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var apiCookie = Session.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(apiCookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={apiCookie}");
            }

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Could not reach the RaceDay API for {Method} {Url}.", method, url);
                return ApiResult<T>.Fail(0, UnreachableMessage);
            }

            using (response)
            {
                // The API renews its sliding session cookie - keep our copy up to date.
                CaptureSessionCookie(response);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ApiSessionExpiredException();
                }

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<T>.Fail(response.StatusCode, await ReadErrorAsync(response));
                }

                if (typeof(T) == typeof(EmptyResponse))
                {
                    return ApiResult<T>.Ok((T)(object)new EmptyResponse(), response.StatusCode);
                }

                try
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                    return data == null
                        ? ApiResult<T>.Fail(response.StatusCode, UnexpectedMessage)
                        : ApiResult<T>.Ok(data, response.StatusCode);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Could not read the API response for {Method} {Url}.", method, url);
                    return ApiResult<T>.Fail(response.StatusCode, UnexpectedMessage);
                }
            }
        }

        /// <summary>Stores (or clears) the API session cookie from a Set-Cookie response header.</summary>
        private void CaptureSessionCookie(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

            foreach (var header in values)
            {
                if (!header.StartsWith(ApiCookieName + "=", StringComparison.Ordinal)) continue;

                var value = header[(ApiCookieName.Length + 1)..];
                var end = value.IndexOf(';');
                if (end >= 0) value = value[..end];

                if (string.IsNullOrEmpty(value))
                {
                    Session.Remove(SessionKeys.ApiCookie);
                }
                else
                {
                    Session.SetString(SessionKeys.ApiCookie, value);
                }
            }
        }

        /// <summary>
        /// Turns an API error response into a short, readable message. The API reports errors as
        /// { "message": "..." } or as ASP.NET validation problem details ({ "errors": { ... } }).
        /// </summary>
        private async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                        {
                            return message.GetString()!;
                        }

                        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        {
                            var messages = errors.EnumerateObject()
                                .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                                    ? p.Value.EnumerateArray().Select(v => v.GetString())
                                    : Enumerable.Empty<string?>())
                                .Where(m => !string.IsNullOrWhiteSpace(m))
                                .Take(3)
                                .ToList();

                            if (messages.Count > 0) return string.Join(" ", messages);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fall through to the generic messages below.
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Forbidden => "You do not have permission to do that.",
                HttpStatusCode.NotFound => "We could not find what you were looking for.",
                HttpStatusCode.Conflict => "That action conflicts with existing data.",
                HttpStatusCode.BadRequest => "Some of the information you entered is not valid.",
                HttpStatusCode.ServiceUnavailable => "This feature is temporarily unavailable. Please try again later.",
                _ => "Something went wrong. Please try again."
            };
        }
    }
}
