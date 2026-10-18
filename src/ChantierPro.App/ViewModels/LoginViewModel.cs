using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _email = "chef.chantier@chantierpro.cm";

    [ObservableProperty]
    private string _password = "Password123!";

    [ObservableProperty]
    private bool _isPasswordHidden = true;

    [ObservableProperty]
    private bool _rememberMe = true;

    public LoginViewModel(IDialogService dialogService) : base(dialogService)
    {
        Title = "Connexion";
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await DialogService.ShowAlertAsync("Erreur de saisie", "Veuillez renseigner votre e-mail et votre mot de passe.");
            return;
        }

        IsBusy = true;
        try
        {
            // Simulation d'authentification rapide
            await Task.Delay(400);
            await Shell.Current.GoToAsync("//dashboard");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BiometricLoginAsync()
    {
        // Simulation de déverrouillage biométrique F-AUT-02
        IsBusy = true;
        try
        {
            await Task.Delay(300);
            await DialogService.ShowToastAsync("Empreinte reconnue. Bienvenue Alain Mvondo !");
            await Shell.Current.GoToAsync("//dashboard");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        var email = await DialogService.ShowPromptAsync("Mot de passe oublié", "Saisissez votre adresse e-mail pour recevoir les instructions de réinitialisation :", initialValue: Email);
        if (!string.IsNullOrWhiteSpace(email))
        {
            await DialogService.ShowAlertAsync("Lien envoyé", $"Un e-mail de réinitialisation a été envoyé à {email}.");
        }
    }
}
