using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

// Use one fixed culture everywhere so numbers and dates behave identically whether the
// app runs locally or inside Docker (decimal points, date formats, etc.).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC ----------
builder.Services.AddControllersWithViews(options =>
{
    // Every POST must carry an anti-forgery token (the form tag helpers add it automatically).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    // If the API reports the session has ended, send the user back to the login page.
    options.Filters.Add<ApiSessionExpiredFilter>();
});

// ---------- Session management ----------
// The server-side session holds the user's identity, role and the API session cookie.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// ---------- Authentication (drives [Authorize] and role-based navigation) ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Web.Auth";
        options.Cookie.HttpOnly = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;

        // The login cookie is only valid while the matching server-side session still exists
        // (for example it is gone after the app restarts) - otherwise the user must log in again.
        options.Events.OnValidatePrincipal = async context =>
        {
            var apiCookie = context.HttpContext.Session.GetString(SessionKeys.ApiCookie);
            if (string.IsNullOrEmpty(apiCookie))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- API client ----------
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("Configuration value 'Api:BaseUrl' is missing. Set it to the address of the RaceDay API.");
}

builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Cookies are managed explicitly through the user's session, never shared between users.
        UseCookies = false
    });

var app = builder.Build();

// ---------- Pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.UseStaticFiles();
app.UseRouting();

app.UseSession();          // must come before authentication (it reads the session)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
