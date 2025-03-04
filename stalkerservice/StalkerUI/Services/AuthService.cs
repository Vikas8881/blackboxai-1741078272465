using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using StalkerUI.Auth;

namespace StalkerUI.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticated();
    Task<LoginResult> Login(LoginRequest request);
    Task<RegisterResult> Register(RegisterRequest request);
    Task Logout();
    Task<UserInfo?> GetUserInfo();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IHttpClientFactory clientFactory,
        AuthenticationStateProvider authStateProvider,
        ILocalStorageService localStorage,
        ILogger<AuthService> logger)
    {
        _httpClient = clientFactory.CreateClient("API");
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<bool> IsAuthenticated()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        return !string.IsNullOrEmpty(token);
    }

    public async Task<LoginResult> Login(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<LoginResult>();

            if (response.IsSuccessStatusCode && result?.Token != null)
            {
                await _localStorage.SetItemAsync("authToken", result.Token);
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);
                return result;
            }

            return new LoginResult 
            { 
                Success = false, 
                Message = result?.Message ?? "An error occurred during login" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new LoginResult 
            { 
                Success = false, 
                Message = "An unexpected error occurred" 
            };
        }
    }

    public async Task<RegisterResult> Register(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<RegisterResult>();

            if (response.IsSuccessStatusCode)
            {
                return result ?? new RegisterResult { Success = true, Message = "Registration successful" };
            }

            return new RegisterResult 
            { 
                Success = false, 
                Message = result?.Message ?? "An error occurred during registration" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new RegisterResult 
            { 
                Success = false, 
                Message = "An unexpected error occurred" 
            };
        }
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<UserInfo?> GetUserInfo()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/user");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfo>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info");
            return null;
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserInfo? User { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsReseller { get; set; }
}

public class RegisterResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public List<string> Roles { get; set; } = new();
}
