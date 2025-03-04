using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using StalkerUI;
using StalkerUI.Auth;
using StalkerUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Configure named HttpClient for API
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001/");
}).AddHttpMessageHandler<AuthenticationHeaderHandler>();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication State Provider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationHeaderHandler>();
builder.Services.AddAuthorizationCore();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IToastService, ToastService>();

// Add custom configuration
builder.Services.AddOptions();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

await builder.Build().RunAsync();

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "USD";
}

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public AuthenticationHeaderHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
