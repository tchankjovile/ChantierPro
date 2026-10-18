using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

[QueryProperty(nameof(ChantierIdString), "id")]
public partial class ChantierEditViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private ChantierAggregate? _existingAggregate;

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private bool _isNew = true;

    [ObservableProperty]
    private string _reference = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _client = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _latitudeText = string.Empty;

    [ObservableProperty]
    private string _longitudeText = string.Empty;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today.AddMonths(6);

    [ObservableProperty]
    private string _totalBudgetText = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    public ChantierEditViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Nouveau chantier";
        Reference = $"CH-{DateTime.Today.Year}-{new Random().Next(100, 999)}";
    }

    async partial void OnChantierIdStringChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
        {
            await LoadForEditAsync(id);
        }
    }

    private async Task LoadForEditAsync(Guid id)
    {
        IsBusy = true;
        try
        {
            _existingAggregate = await _repository.GetChantierAsync(id);
            if (_existingAggregate != null)
            {
                IsNew = false;
                Title = "Modifier le chantier";
                var c = _existingAggregate.Chantier;
                Reference = c.Reference;
                Name = c.Name;
                Client = c.Client;
                Address = c.Address;
                LatitudeText = c.Latitude?.ToString() ?? string.Empty;
                LongitudeText = c.Longitude?.ToString() ?? string.Empty;
                StartDate = c.StartDate;
                EndDate = c.PlannedEndDate;
                TotalBudgetText = c.TotalBudget.ToString("0");
                Description = c.Description;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GetCurrentLocationAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
            if (location != null)
            {
                LatitudeText = location.Latitude.ToString("F6");
                LongitudeText = location.Longitude.ToString("F6");
                await DialogService.ShowToastAsync("Coordonnées GPS capturées");
            }
            else
            {
                // Coordonnées de test réalistes Douala / Yaoundé
                LatitudeText = "4.051056";
                LongitudeText = "9.767868";
                await DialogService.ShowToastAsync("Coordonnées GPS par défaut (Akwa)");
            }
        }
        catch (Exception)
        {
            LatitudeText = "4.051056";
            LongitudeText = "9.767868";
            await DialogService.ShowToastAsync("GPS simulé : Akwa, Douala");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez saisir le nom du chantier.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Client))
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez renseigner le client / maître d'ouvrage.");
            return;
        }

        if (!decimal.TryParse(TotalBudgetText.Replace(" ", "").Replace("\u00a0", ""), out var budget) || budget <= 0)
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez saisir un budget global valide supérieur à 0.");
            return;
        }

        if (EndDate < StartDate)
        {
            await DialogService.ShowAlertAsync("Validation", "La date de fin prévue doit être postérieure à la date de début.");
            return;
        }

        double? lat = double.TryParse(LatitudeText, out var parsedLat) ? parsedLat : null;
        double? lon = double.TryParse(LongitudeText, out var parsedLon) ? parsedLon : null;

        IsBusy = true;
        try
        {
            if (IsNew)
            {
                var newChantier = new Chantier
                {
                    Id = Guid.NewGuid(),
                    Reference = string.IsNullOrWhiteSpace(Reference) ? "CH-AUTO" : Reference.Trim(),
                    Name = Name.Trim(),
                    Client = Client.Trim(),
                    Address = Address.Trim(),
                    Latitude = lat,
                    Longitude = lon,
                    StartDate = StartDate,
                    PlannedEndDate = EndDate,
                    TotalBudget = budget,
                    Status = ChantierStatus.Planifie,
                    Description = Description.Trim()
                };

                // Génération automatique des phases modèles & budget par poste (F-PHA-01, F-DEP-01)
                var phases = ChantierTemplates.DefaultPhases(newChantier.Id, StartDate, EndDate, budget);
                var budgetLines = ChantierTemplates.DefaultBudgetLines(newChantier.Id, budget);

                var aggregate = new ChantierAggregate
                {
                    Chantier = newChantier,
                    Phases = phases,
                    BudgetLines = budgetLines
                };

                await _repository.SaveChantierAsync(aggregate);
                await DialogService.ShowToastAsync("Chantier créé avec ses phases et son budget !");
                await Shell.Current.GoToAsync($"chantier-detail?id={newChantier.Id}");
            }
            else if (_existingAggregate != null)
            {
                var c = _existingAggregate.Chantier;
                c.Reference = Reference.Trim();
                c.Name = Name.Trim();
                c.Client = Client.Trim();
                c.Address = Address.Trim();
                c.Latitude = lat;
                c.Longitude = lon;
                c.StartDate = StartDate;
                c.PlannedEndDate = EndDate;
                c.TotalBudget = budget;
                c.Description = Description.Trim();

                await _repository.SaveChantierAsync(_existingAggregate);
                await DialogService.ShowToastAsync("Modifications enregistrées");
                await Shell.Current.GoToAsync("..");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
