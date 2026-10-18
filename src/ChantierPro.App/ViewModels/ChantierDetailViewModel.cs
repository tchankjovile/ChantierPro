using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Helpers;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

[QueryProperty(nameof(ChantierIdString), "id")]
public partial class ChantierDetailViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private ChantierAggregate? _aggregate;
    private ChantierMetrics? _metrics;

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private Chantier? _chantier;

    [ObservableProperty]
    private string _statusLabel = string.Empty;

    [ObservableProperty]
    private Tone _statusTone = Tone.Neutral;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private double _expectedProgress;

    [ObservableProperty]
    private decimal _consumedBudget;

    [ObservableProperty]
    private decimal _remainingBudget;

    [ObservableProperty]
    private double _consumptionRate;

    [ObservableProperty]
    private Tone _budgetTone = Tone.Success;

    [ObservableProperty]
    private decimal _estimateAtCompletion;

    [ObservableProperty]
    private HealthLevel _health = HealthLevel.Good;

    [ObservableProperty]
    private int _healthScore = 100;

    [ObservableProperty]
    private string _healthLabel = string.Empty;

    [ObservableProperty]
    private Tone _healthTone = Tone.Success;

    [ObservableProperty]
    private bool _hasAlerts;

    [ObservableProperty]
    private int _alertsCount;

    [ObservableProperty]
    private bool _isReadOnly;

    public ObservableCollection<Phase> Phases { get; } = new();
    public ObservableCollection<BudgetLineResult> TopBudgetLines { get; } = new();
    public ObservableCollection<AlertItem> ActiveAlerts { get; } = new();

    public ChantierDetailViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Fiche chantier";
    }

    async partial void OnChantierIdStringChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
        {
            await LoadChantierAsync(id);
        }
    }

    public async Task LoadChantierAsync(Guid id)
    {
        IsBusy = true;
        try
        {
            _aggregate = await _repository.GetChantierAsync(id);
            if (_aggregate == null)
            {
                await DialogService.ShowAlertAsync("Erreur", "Chantier introuvable.");
                return;
            }

            Chantier = _aggregate.Chantier;
            Title = Chantier.Name;
            IsReadOnly = Chantier.IsReadOnly;
            StatusLabel = Labels.Of(Chantier.Status);
            StatusTone = Palette.ToneFor(Chantier.Status);

            var materials = await _repository.GetMaterialsAsync();
            var insight = ChantierInsightService.Analyze(_aggregate, materials, DateTime.Today);
            _metrics = insight.Metrics;

            Progress = (double)_metrics.Progress;
            ExpectedProgress = (double)_metrics.ExpectedProgress;
            ConsumedBudget = _metrics.Consumed;
            RemainingBudget = _metrics.Remaining;
            ConsumptionRate = (double)(_metrics.ConsumptionRate * 100m);
            BudgetTone = Palette.ToneForRate(_metrics.ConsumptionRate);
            EstimateAtCompletion = _metrics.EstimateAtCompletion;

            Health = _metrics.Health;
            HealthScore = _metrics.HealthScore;
            HealthLabel = Labels.Of(_metrics.Health);
            HealthTone = Palette.ToneFor(_metrics.Health);

            Phases.Clear();
            foreach (var p in _aggregate.Phases.OrderBy(x => x.Order))
            {
                Phases.Add(p);
            }

            TopBudgetLines.Clear();
            foreach (var l in _metrics.Lines.OrderByDescending(x => x.Actual).Take(4))
            {
                TopBudgetLines.Add(l);
            }

            ActiveAlerts.Clear();
            foreach (var a in insight.Alerts.Take(3))
            {
                ActiveAlerts.Add(a);
            }
            HasAlerts = ActiveAlerts.Count > 0;
            AlertsCount = insight.Alerts.Count;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditChantierAsync()
    {
        if (Chantier == null) return;
        if (IsReadOnly)
        {
            await DialogService.ShowAlertAsync("Lecture seule", "Ce chantier est clôturé et ne peut plus être modifié (RG-07).");
            return;
        }
        await Shell.Current.GoToAsync($"chantier-edit?id={Chantier.Id}");
    }

    [RelayCommand]
    private async Task OpenPhasesAsync()
    {
        if (Chantier == null) return;
        await Shell.Current.GoToAsync($"phases?id={Chantier.Id}");
    }

    [RelayCommand]
    private async Task OpenStockAsync()
    {
        if (Chantier == null) return;
        await Shell.Current.GoToAsync($"stock?id={Chantier.Id}");
    }

    [RelayCommand]
    private async Task OpenBudgetAsync()
    {
        if (Chantier == null) return;
        await Shell.Current.GoToAsync($"budget?id={Chantier.Id}");
    }

    [RelayCommand]
    private async Task OpenJournalAsync()
    {
        if (Chantier == null) return;
        await Shell.Current.GoToAsync($"journal?id={Chantier.Id}");
    }

    [RelayCommand]
    private async Task DuplicateChantierAsync()
    {
        if (Chantier == null) return;
        var confirm = await DialogService.ShowConfirmAsync("Dupliquer le chantier", $"Voulez-vous dupliquer « {Chantier.Name} » comme modèle ?", "Dupliquer", "Annuler");
        if (confirm)
        {
            var copy = await _repository.DuplicateChantierAsync(Chantier.Id);
            await DialogService.ShowToastAsync($"Chantier dupliqué : {copy.Chantier.Name}");
            await Shell.Current.GoToAsync($"chantier-detail?id={copy.Chantier.Id}");
        }
    }

    [RelayCommand]
    private async Task NavigateGpsAsync()
    {
        if (Chantier?.Latitude == null || Chantier?.Longitude == null)
        {
            await DialogService.ShowAlertAsync("GPS", "Aucune coordonnée GPS enregistrée pour ce chantier.");
            return;
        }

        try
        {
            var location = new Location(Chantier.Latitude.Value, Chantier.Longitude.Value);
            var options = new MapLaunchOptions { Name = Chantier.Name, NavigationMode = NavigationMode.Driving };
            await Map.Default.OpenAsync(location, options);
        }
        catch (Exception)
        {
            await DialogService.ShowAlertAsync("GPS", $"Navigation vers : {Chantier.Address} ({Chantier.Latitude}, {Chantier.Longitude})");
        }
    }

    [RelayCommand]
    private async Task ChangeStatusAsync()
    {
        if (Chantier == null) return;
        var choice = await DialogService.ShowActionSheetAsync("Modifier le statut", "Annuler", null,
            "Planifié", "En cours", "Suspendu", "Terminé", "Clôturé (Lecture seule)");

        if (choice == null || choice == "Annuler") return;

        ChantierStatus newStatus = choice switch
        {
            "Planifié" => ChantierStatus.Planifie,
            "En cours" => ChantierStatus.EnCours,
            "Suspendu" => ChantierStatus.Suspendu,
            "Terminé" => ChantierStatus.Termine,
            "Clôturé (Lecture seule)" => ChantierStatus.Cloture,
            _ => Chantier.Status
        };

        await _repository.SetStatusAsync(Chantier.Id, newStatus);
        Chantier.Status = newStatus;
        StatusLabel = Labels.Of(newStatus);
        StatusTone = Palette.ToneFor(newStatus);
        IsReadOnly = Chantier.IsReadOnly;
        await DialogService.ShowToastAsync($"Statut mis à jour : {StatusLabel}");
    }
}
