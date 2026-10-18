namespace ChantierPro.Mobile.Services;

public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non");
    Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons);
    Task<string?> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Annuler", string? placeholder = null, int maxLength = -1, Keyboard? keyboard = null, string initialValue = "");
    Task ShowToastAsync(string message);
}

public class DialogService : IDialogService
{
    private static Page? CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page;

    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (CurrentPage is Page page)
        {
            await page.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non")
    {
        if (CurrentPage is Page page)
        {
            return await page.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }

    public async Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
    {
        if (CurrentPage is Page page)
        {
            return await page.DisplayActionSheet(title, cancel, destruction, buttons);
        }
        return null;
    }

    public async Task<string?> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Annuler", string? placeholder = null, int maxLength = -1, Keyboard? keyboard = null, string initialValue = "")
    {
        if (CurrentPage is Page page)
        {
            return await page.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard, initialValue);
        }
        return null;
    }

    public async Task ShowToastAsync(string message)
    {
        // Simple toast fallback via snackbar or lightweight alert
        if (CurrentPage is Page page)
        {
            await page.DisplayAlert("Information", message, "OK");
        }
    }
}
