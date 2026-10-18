using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _userRole = string.Empty;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isTerrainMode;

    [ObservableProperty]
    private string _selectedCurrency = "FCFA (XAF)";

    [ObservableProperty]
    private string _selectedLanguage = "Français";

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _biometricsEnabled = true;

    public SettingsViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Profil & Paramètres";

        var u = _repository.CurrentUser;
        UserName = u.Name;
        UserEmail = u.Email;
        UserRole = Labels.Of(u.Role);

        IsDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }
    }

    partial void OnIsTerrainModeChanged(bool value)
    {
        // En mode Terrain (§5) : cibles tactiles élargies à 60 dp
        if (Application.Current?.Resources != null)
        {
            Application.Current.Resources["TouchTarget"] = value ? 60.0 : 48.0;
        }
    }

    [RelayCommand]
    private async Task ChangeCurrencyAsync()
    {
        var choice = await DialogService.ShowActionSheetAsync("Choisir la devise", "Annuler", null, "FCFA (XAF)", "EUR (€)", "USD ($)");
        if (choice != null && choice != "Annuler")
        {
            SelectedCurrency = choice;
            await DialogService.ShowToastAsync($"Devise sélectionnée : {choice}");
        }
    }

    [RelayCommand]
    private async Task ChangeLanguageAsync()
    {
        var choice = await DialogService.ShowActionSheetAsync("Choisir la langue", "Annuler", null, "Français", "English");
        if (choice != null && choice != "Annuler")
        {
            SelectedLanguage = choice;
            await DialogService.ShowToastAsync($"Langue sélectionnée : {choice}");
        }
    }

    [RelayCommand]
    private async Task BackupDataAsync()
    {
        IsBusy = true;
        try
        {
            await Task.Delay(500); // Simulation backup
            await DialogService.ShowAlertAsync("Sauvegarde réussie", "Toutes les bases locales (chantiers, mouvements, pointages) ont été archivées avec succès dans un fichier de secours chiffré.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreDataAsync()
    {
        var confirm = await DialogService.ShowConfirmAsync("Restauration", "Voulez-vous restaurer les données de démonstration d'origine ?", "Restaurer", "Annuler");
        if (confirm)
        {
            await DialogService.ShowToastAsync("Données restaurées");
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await DialogService.ShowConfirmAsync("Déconnexion", "Êtes-vous sûr de vouloir vous déconnecter ?", "Déconnexion", "Annuler");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
