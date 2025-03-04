using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StalkerApi.Repositories;
using StalkerModels.Models;

namespace StalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IRepository<ProductPrice> _productPriceRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(
        IRepository<Currency> currencyRepository,
        IRepository<ProductPrice> productPriceRepository,
        IConfiguration configuration,
        ILogger<CurrencyController> logger)
    {
        _currencyRepository = currencyRepository;
        _productPriceRepository = productPriceRepository;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Currency>>> GetCurrencies([FromQuery] bool activeOnly = true)
    {
        try
        {
            var currencies = (await _currencyRepository.GetAllAsync())
                .Where(c => !activeOnly || c.IsActive)
                .OrderBy(c => c.Code)
                .ToList();

            return Ok(currencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving currencies");
            return StatusCode(500, "Internal server error while retrieving currencies");
        }
    }

    [HttpGet("default")]
    public async Task<ActionResult<Currency>> GetDefaultCurrency()
    {
        try
        {
            var defaultCurrency = (await _currencyRepository.GetAllAsync())
                .FirstOrDefault(c => c.IsDefault && c.IsActive);

            if (defaultCurrency == null)
            {
                // Fallback to configuration
                defaultCurrency = new Currency
                {
                    Code = _configuration["DefaultCurrency:Code"] ?? "USD",
                    Name = _configuration["DefaultCurrency:Name"] ?? "US Dollar",
                    Symbol = _configuration["DefaultCurrency:Symbol"] ?? "$",
                    ExchangeRate = 1,
                    IsActive = true,
                    IsDefault = true
                };
            }

            return Ok(defaultCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default currency");
            return StatusCode(500, "Internal server error while retrieving default currency");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Currency>> CreateCurrency([FromBody] Currency currency)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate currency code
            if (!IsValidCurrencyCode(currency.Code))
                return BadRequest("Invalid currency code");

            // Check if currency already exists
            var existing = (await _currencyRepository.GetAllAsync())
                .FirstOrDefault(c => c.Code == currency.Code);

            if (existing != null)
                return BadRequest("Currency already exists");

            // If this is marked as default, unset other default currencies
            if (currency.IsDefault)
            {
                var currentDefault = (await _currencyRepository.GetAllAsync())
                    .FirstOrDefault(c => c.IsDefault);

                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                    await _currencyRepository.UpdateAsync(currentDefault);
                }
            }

            var created = await _currencyRepository.AddAsync(currency);
            return CreatedAtAction(nameof(GetCurrency), new { code = created.Code }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating currency");
            return StatusCode(500, "Internal server error while creating currency");
        }
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<Currency>> GetCurrency(string code)
    {
        try
        {
            var currency = (await _currencyRepository.GetAllAsync())
                .FirstOrDefault(c => c.Code == code.ToUpper());

            if (currency == null)
                return NotFound();

            return Ok(currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving currency {Code}", code);
            return StatusCode(500, "Internal server error while retrieving currency");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{code}")]
    public async Task<IActionResult> UpdateCurrency(string code, [FromBody] Currency currency)
    {
        try
        {
            if (code.ToUpper() != currency.Code.ToUpper())
                return BadRequest("Currency code mismatch");

            var existing = (await _currencyRepository.GetAllAsync())
                .FirstOrDefault(c => c.Code == code.ToUpper());

            if (existing == null)
                return NotFound();

            // If this is being marked as default, unset other default currencies
            if (currency.IsDefault && !existing.IsDefault)
            {
                var currentDefault = (await _currencyRepository.GetAllAsync())
                    .FirstOrDefault(c => c.IsDefault && c.Id != existing.Id);

                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                    await _currencyRepository.UpdateAsync(currentDefault);
                }
            }

            existing.Name = currency.Name;
            existing.Symbol = currency.Symbol;
            existing.ExchangeRate = currency.ExchangeRate;
            existing.IsActive = currency.IsActive;
            existing.IsDefault = currency.IsDefault;
            existing.Format = currency.Format;

            await _currencyRepository.UpdateAsync(existing);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency {Code}", code);
            return StatusCode(500, "Internal server error while updating currency");
        }
    }

    [HttpGet("convert")]
    public async Task<ActionResult<CurrencyConversionResult>> ConvertCurrency(
        [FromQuery] string fromCurrency,
        [FromQuery] string toCurrency,
        [FromQuery] decimal amount)
    {
        try
        {
            var currencies = await _currencyRepository.GetAllAsync();
            
            var sourceCurrency = currencies.FirstOrDefault(c => c.Code == fromCurrency.ToUpper());
            var targetCurrency = currencies.FirstOrDefault(c => c.Code == toCurrency.ToUpper());

            if (sourceCurrency == null || targetCurrency == null)
                return BadRequest("Invalid currency code");

            if (!sourceCurrency.IsActive || !targetCurrency.IsActive)
                return BadRequest("Currency is not active");

            // Convert through base currency (usually USD) if needed
            decimal convertedAmount;
            if (sourceCurrency.IsDefault)
            {
                convertedAmount = amount * targetCurrency.ExchangeRate;
            }
            else if (targetCurrency.IsDefault)
            {
                convertedAmount = amount / sourceCurrency.ExchangeRate;
            }
            else
            {
                // Convert to base currency first, then to target currency
                var inBaseCurrency = amount / sourceCurrency.ExchangeRate;
                convertedAmount = inBaseCurrency * targetCurrency.ExchangeRate;
            }

            return Ok(new CurrencyConversionResult
            {
                FromCurrency = fromCurrency.ToUpper(),
                ToCurrency = toCurrency.ToUpper(),
                Amount = amount,
                ConvertedAmount = convertedAmount,
                ExchangeRate = targetCurrency.ExchangeRate / sourceCurrency.ExchangeRate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency");
            return StatusCode(500, "Internal server error while converting currency");
        }
    }

    private bool IsValidCurrencyCode(string code)
    {
        // Currency codes should be 3 uppercase letters
        return code.Length == 3 && code.All(char.IsLetter) && code.All(char.IsUpper);
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
