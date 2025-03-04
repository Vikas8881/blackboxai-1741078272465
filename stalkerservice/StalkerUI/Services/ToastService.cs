using StalkerUI.Shared;

namespace StalkerUI.Services;

public interface IToastService
{
    event Action<string, ToastType, string?, int>? OnShow;
    void ShowSuccess(string message, string? title = null, int duration = 5000);
    void ShowError(string message, string? title = null, int duration = 5000);
    void ShowWarning(string message, string? title = null, int duration = 5000);
    void ShowInfo(string message, string? title = null, int duration = 5000);
}

public class ToastService : IToastService
{
    public event Action<string, ToastType, string?, int>? OnShow;

    public void ShowSuccess(string message, string? title = null, int duration = 5000)
    {
        OnShow?.Invoke(message, ToastType.Success, title, duration);
    }

    public void ShowError(string message, string? title = null, int duration = 5000)
    {
        OnShow?.Invoke(message, ToastType.Error, title, duration);
    }

    public void ShowWarning(string message, string? title = null, int duration = 5000)
    {
        OnShow?.Invoke(message, ToastType.Warning, title, duration);
    }

    public void ShowInfo(string message, string? title = null, int duration = 5000)
    {
        OnShow?.Invoke(message, ToastType.Info, title, duration);
    }
}
