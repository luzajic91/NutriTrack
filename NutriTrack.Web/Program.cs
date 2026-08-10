using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NutriTrack.Web;
using NutriTrack.Web.Services;
using NutriTrack.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Client-side logging goes to the browser console. Keep app logs at Information
// and silence the framework's noisier categories.
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5072")
});

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication. TokenStore holds the access token in memory for the life of the page;
// the refresh token is an HttpOnly cookie the client never touches.
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

// Add Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();

var host = builder.Build();

// Tokens from before the cookie change are still sitting in localStorage, where a script can
// read them. Clear them once on startup. Remove this — and the Blazored.LocalStorage package,
// which nothing else uses — once users have cycled through.
var localStorage = host.Services.GetRequiredService<ILocalStorageService>();
await localStorage.RemoveItemAsync("accessToken");
await localStorage.RemoveItemAsync("refreshToken");

await host.RunAsync();