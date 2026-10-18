using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Helpers;
using ChantierPro.Mobile.Models;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public class ChantierCardItem
{
    public ChantierAggregate Aggregate { get; }
    public Chantier MetricsChantier => Aggregate.Chantier;
    public ChantierMetrics Metrics { get; }

    public string Name => Aggregate.Chantier.Name;
    public string Reference => Aggregate.Chantier.Reference;
    public string Client => Aggregate.Chantier.Client;
    public string Address => Aggregate.Chantier.Address;
    public string StatusLabel => Labels.Of(Aggregate.Chantier.Status);
    public Tone StatusTone => Palette.ToneFor(Aggregate.Chantier.Status);

    public double Progress => (double)Metrics.Progress;
    public double ExpectedProgress => (double)Metrics.ExpectedProgress;
    public decimal TotalBudget => Aggregate.Chantier.TotalBudget;
    public decimal ConsumedBudget => Metrics.Consumed;
    public decimal RemainingBudget => Metrics.Remaining;
    public double ConsumptionRate => (double)(Metrics.ConsumptionRate * 100m);
    public Tone BudgetTone => Palette.ToneForRate(Metrics.ConsumptionRate);

    public HealthLevel Health => Metrics.Health;
    public Tone HealthTone => Palette.ToneFor(Metrics.Health);
    public string HealthLabel => Labels.Of(Metrics.Health);
    public int HealthScore => Metrics.HealthScore;

    public ChantierCardItem(ChantierAggregate aggregate, ChantierMetrics metrics)
    {
        Aggregate = aggregate;
        Metrics = metrics;
    }
}

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private List<ChantierCardItem> _allItems = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private decimal _totalBudgetConsolidated;

    [ObservableProperty]
    private decimal _totalSpentConsolidated;

    [ObservableProperty]
    private decimal _totalRemainingConsolidated;

    [ObservableProperty]
    private double _averageProgressConsolidated;

    [ObservableProperty]
    private int _activeAlertsCount;

    [ObservableProperty]
    private int _criticalAlertsCount;

    [ObservableProperty]
    private AlertItem? _topAlert;

    public ObservableCollection<ChantierCardItem> FilteredChantiers { get; } = new();
    public ObservableCollection<ChipItem> StatusFilters { get; } = new();

    private ChantierStatus? _selectedStatusFilter = null;

    public DashboardViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Tableau de bord";
        InitFilters();
    }

    private void InitFilters()
    {
        StatusFilters.Add(new ChipItem("Tous", null, OnFilterSelected, isSelected: true));
        StatusFilters.Add(new ChipItem("En cours", ChantierStatus.EnCours, OnFilterSelected));
        StatusFilters.Add(new ChipItem("Planifiés", ChantierStatus.Planifie, OnFilterSelected));
        StatusFilters.Add(new ChipItem("Suspendus", ChantierStatus.Suspendu, OnFilterSelected));
        StatusFilters.Add(new ChipItem("Terminés", ChantierStatus.Termine, OnFilterSelected));
    }

    private void OnFilterSelected(ChipItem chip)
    {
        foreach (var c in StatusFilters) c.IsSelected = (c == chip);
        _selectedStatusFilter = chip.Value as ChantierStatus?;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    public override async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var aggregates = await _repository.GetChantiersAsync();
            var materials = await _repository.GetMaterialsAsync();
            var alerts = await _repository.GetAlertsAsync();

            _allItems.Clear();
            decimal totalBudget = 0m;
            decimal totalSpent = 0m;
            double progressSum = 0.0;

            foreach (var agg in aggregates)
            {
                var insight = ChantierInsightService.Analyze(agg, materials, DateTime.Today);
                var item = new ChantierCardItem(agg, insight.Metrics);
                _allItems.Add(item);

                totalBudget += item.TotalBudget;
                totalSpent += item.ConsumedBudget;
                progressSum += item.Progress;
            }

            TotalBudgetConsolidated = totalBudget;
            TotalSpentConsolidated = totalSpent;
            TotalRemainingConsolidated = totalBudget - totalSpent;
            AverageProgressConsolidated = _allItems.Count > 0 ? Math.Round(progressSum / _allItems.Count, 1) : 0.0;

            ActiveAlertsCount = alerts.Count;
            CriticalAlertsCount = alerts.Count(a => a.Level == AlertLevel.Critical);
            TopAlert = alerts.FirstOrDefault();

            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var query = _allItems.AsEnumerable();

        if (_selectedStatusFilter.HasValue)
        {
            query = query.Where(i => i.Aggregate.Chantier.Status == _selectedStatusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim().ToLowerInvariant();
            query = query.Where(i =>
                i.Name.ToLowerInvariant().Contains(term) ||
                i.Reference.ToLowerInvariant().Contains(term) ||
                i.Client.ToLowerInvariant().Contains(term) ||
                i.Address.ToLowerInvariant().Contains(term));
        }

        FilteredChantiers.Clear();
        foreach (var item in query)
        {
            FilteredChantiers.Add(item);
        }
    }

    [RelayCommand]
    private async Task OpenChantierDetailAsync(ChantierCardItem? item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"chantier-detail?id={item.Aggregate.Chantier.Id}");
    }

    [RelayCommand]
    private async Task CreateChantierAsync()
    {
        await Shell.Current.GoToAsync("chantier-edit");
    }

    [RelayCommand]
    private async Task QuickAttendanceAsync()
    {
        await Shell.Current.GoToAsync("//attendance");
    }

    [RelayCommand]
    private async Task QuickStockMovementAsync()
    {
        await Shell.Current.GoToAsync("stock-movement-edit");
    }

    [RelayCommand]
    private async Task QuickExpenseAsync()
    {
        await Shell.Current.GoToAsync("expense-edit");
    }

    [RelayCommand]
    private async Task OpenAlertsAsync()
    {
        await Shell.Current.GoToAsync("alerts");
    }
}
