namespace GameCompanion.Engine.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Base class for all ViewModels in the application.
/// Provides common functionality like loading state and error handling.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Sets the status message for display to the user.
    /// </summary>
    protected void SetStatus(string message)
    {
        StatusMessage = message;
        HasError = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// Sets an error state with a message.
    /// </summary>
    protected void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        StatusMessage = null;
    }

    /// <summary>
    /// Clears both status and error messages.
    /// </summary>
    protected void ClearMessages()
    {
        StatusMessage = null;
        HasError = false;
        ErrorMessage = null;
    }
}
