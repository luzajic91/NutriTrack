using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NutriTrack.Web;
using NutriTrack.Web.Services;
using NutriTrack.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5072")
});

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

// Add Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();

await builder.Build().RunAsync();