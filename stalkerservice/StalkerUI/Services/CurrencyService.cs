using Blazored.LocalStorage;
using StalkerModels.Models;
using System.Net.Http.Json;

namespace StalkerUI.Services;

public interface ICurrencyService
{
    event Action? OnCurrencyChanged;
    Task<List<Currency>> GetAvailableCurrencies();
    Task<Currency> GetDefaultCurrency();
    Task<Currency> GetCurrentCurrency();
    Task SetCurrentCurrency(string currencyCode);
    Task<decimal> ConvertPrice(decimal amount, string fromCurrency, string toCurrency);
    string FormatPrice(decimal price, Currency? currency = null);
}

public class CurrencyService : ICurrencyService
{
    private const string CURRENCY_STORAGE_KEY = "selected_currency";
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<CurrencyService> _logger;
    private Currency? _currentCurrency;
    private List<Currency>? _availableCurrencies;

    public event Action? OnCurrencyChanged;

    public CurrencyService(
        IHttpClientFactory clientFactory,
        ILocalStorageService localStorage,
        ILogger<CurrencyService> logger)
    {
        _clientFactory = clientFactory;
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<List<Currency>> GetAvailableCurrencies()
    {
        try
        {
            if (_availableCurrencies != null)
                return _availableCurrencies;

            var client = _clientFactory.CreateClient("API");
            var currencies = await client.GetFromJsonAsync<List<Currency>>("api/currency") ?? new List<Currency>();
            _availableCurrencies = currencies;
            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available currencies");
            return new List<Currency>();
        }
    }

    public async Task<Currency> GetDefaultCurrency()
    {
        try
        {
            var client = _clientFactory.CreateClient("API");
            var defaultCurrency = await client.GetFromJsonAsync<Currency>("api/currency/default");
            return defaultCurrency ?? new Currency { Code = "USD", Symbol = "$", Name = "US Dollar", IsDefault = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching default currency");
            return new Currency { Code = "USD", Symbol = "$", Name = "US Dollar", IsDefault = true };
        }
    }

    public async Task<Currency> GetCurrentCurrency()
    {
        try
        {
            if (_currentCurrency != null)
                return _currentCurrency;

            var storedCode = await _localStorage.GetItemAsync<string>(CURRENCY_STORAGE_KEY);
            if (string.IsNullOrEmpty(storedCode))
            {
                var defaultCurrency = await GetDefaultCurrency();
                _currentCurrency = defaultCurrency;
                return defaultCurrency;
            }

            var currencies = await GetAvailableCurrencies();
            var currency = currencies.FirstOrDefault(c => c.Code == storedCode);
            if (currency == null)
            {
                var defaultCurrency = await GetDefaultCurrency();
                _currentCurrency = defaultCurrency;
                return defaultCurrency;
            }

            _currentCurrency = currency;
            return currency;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current currency");
            return new Currency { Code = "USD", Symbol = "$", Name = "US Dollar", IsDefault = true };
        }
    }

    public async Task SetCurrentCurrency(string currencyCode)
    {
        try
        {
            var currencies = await GetAvailableCurrencies();
            var currency = currencies.FirstOrDefault(c => c.Code == currencyCode);
            if (currency != null)
            {
                await _localStorage.SetItemAsync(CURRENCY_STORAGE_KEY, currencyCode);
                _currentCurrency = currency;
                OnCurrencyChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting current currency");
            throw;
        }
    }

    public async Task<decimal> ConvertPrice(decimal amount, string fromCurrency, string toCurrency)
    {
        try
        {
            if (fromCurrency == toCurrency)
                return amount;

            var client = _clientFactory.CreateClient("API");
            var response = await client.GetFromJsonAsync<CurrencyConversionResult>(
                $"api/currency/convert?fromCurrency={fromCurrency}&toCurrency={toCurrency}&amount={amount}");

            return response?.ConvertedAmount ?? amount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting price");
            return amount;
        }
    }

    public string FormatPrice(decimal price, Currency? currency = null)
    {
        try
        {
            currency ??= _currentCurrency ?? new Currency { Code = "USD", Symbol = "$", Name = "US Dollar", Format = "{symbol}{price}" };
            var format = currency.Format ?? "{symbol}{price}";
            var formattedPrice = price.ToString("N2");

            return format
                .Replace("{symbol}", currency.Symbol)
                .Replace("{price}", formattedPrice)
                .Replace("{code}", currency.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting price");
            return $"${price:N2}";
        }
    }
}

public class CurrencyConversionResult
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal ExchangeRate { get; set; }
}
