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
// ⚠️ IMPORTANT: Update this to match your API URL
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5072") // Your NutriTrack.Api URL
});

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

// Add Application Services (Only AuthService for now - Phase 1)
builder.Services.AddScoped<IAuthService, AuthService>();

// TODO: Add these in Phase 2 when we implement Recipes and Meals
// builder.Services.AddScoped<IRecipeService, RecipeService>();
// builder.Services.AddScoped<IMealService, MealService>();
// builder.Services.AddScoped<IFoodService, FoodService>();

await builder.Build().RunAsync();