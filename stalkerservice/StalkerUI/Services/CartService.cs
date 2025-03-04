using Blazored.LocalStorage;
using StalkerModels.Models;

namespace StalkerUI.Services;

public interface ICartService
{
    event Action? OnChange;
    Task<int> GetCartItemCount();
    Task<List<CartItem>> GetCartItems();
    Task AddToCart(Product product, int quantity = 1);
    Task UpdateQuantity(int productId, int quantity);
    Task RemoveFromCart(int productId);
    Task ClearCart();
    Task<decimal> GetTotal();
}

public class CartService : ICartService
{
    private const string CART_STORAGE_KEY = "shopping_cart";
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<CartService> _logger;

    public event Action? OnChange;

    public CartService(ILocalStorageService localStorage, ILogger<CartService> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<int> GetCartItemCount()
    {
        try
        {
            var cart = await GetCartItems();
            return cart.Sum(item => item.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart item count");
            return 0;
        }
    }

    public async Task<List<CartItem>> GetCartItems()
    {
        try
        {
            return await _localStorage.GetItemAsync<List<CartItem>>(CART_STORAGE_KEY) ?? new List<CartItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart items");
            return new List<CartItem>();
        }
    }

    public async Task AddToCart(Product product, int quantity = 1)
    {
        try
        {
            var cart = await GetCartItems();
            var existingItem = cart.FirstOrDefault(item => item.ProductId == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.DiscountedPrice ?? product.BasePrice,
                    Quantity = quantity,
                    ImageUrl = product.MainImage
                });
            }

            await _localStorage.SetItemAsync(CART_STORAGE_KEY, cart);
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart");
            throw;
        }
    }

    public async Task UpdateQuantity(int productId, int quantity)
    {
        try
        {
            var cart = await GetCartItems();
            var item = cart.FirstOrDefault(item => item.ProductId == productId);

            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.Remove(item);
                }

                await _localStorage.SetItemAsync(CART_STORAGE_KEY, cart);
                OnChange?.Invoke();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item quantity");
            throw;
        }
    }

    public async Task RemoveFromCart(int productId)
    {
        try
        {
            var cart = await GetCartItems();
            var item = cart.FirstOrDefault(item => item.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                await _localStorage.SetItemAsync(CART_STORAGE_KEY, cart);
                OnChange?.Invoke();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart");
            throw;
        }
    }

    public async Task ClearCart()
    {
        try
        {
            await _localStorage.RemoveItemAsync(CART_STORAGE_KEY);
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            throw;
        }
    }

    public async Task<decimal> GetTotal()
    {
        try
        {
            var cart = await GetCartItems();
            return cart.Sum(item => item.Price * item.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cart total");
            return 0;
        }
    }
}

public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
}
