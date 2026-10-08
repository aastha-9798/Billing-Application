namespace PlateBilling.ViewModels;

/// <summary>
/// A screen's view model that reloads shared data (clients, plate types,
/// rates, ...) when the user returns to the screen, keeping what the
/// user has entered or selected.
/// </summary>
public interface IRefreshable
{
    Task RefreshAsync();
}
