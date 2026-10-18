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

public class AlertItemVm : ObservableObject
{
    public AlertItem Alert { get; }
    public string Message => Alert.Message;
    public DateTime Date => Alert.Date;
    public AlertLevel Level => Alert.Level;
    public AlertType Type => Alert.Type;
    public string TypeLabel => Labels.Of(Alert.Type);
    public string LevelLabel => Labels.Of(Alert.Level);
    public Tone Tone => Palette.ToneFor(Alert.Level);
    public string IconGlyph => Palette.IconFor(Alert.Type);

    public bool IsRead
    {
        get => Alert.IsRead;
        set
        {
            if (Alert.IsRead != value)
            {
                Alert.IsRead = value;
                OnPropertyChanged();
            }
        }
    }

    public AlertItemVm(AlertItem alert)
    {
        Alert = alert;
    }
}

public partial class AlertsViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private List<AlertItemVm> _allAlerts = new();

    public ObservableCollection<AlertItemVm> FilteredAlerts { get; } = new();
    public ObservableCollection<ChipItem> TypeFilters { get; } = new();

    private AlertType? _selectedTypeFilter = null;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private int _criticalCount;

    public AlertsViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Centre d'alertes";
        InitFilters();
    }

    private void InitFilters()
    {
        TypeFilters.Add(new ChipItem("Toutes", null, OnFilterSelected, isSelected: true));
        TypeFilters.Add(new ChipItem("Budget", AlertType.Budget, OnFilterSelected));
        TypeFilters.Add(new ChipItem("Stock", AlertType.Stock, OnFilterSelected));
        TypeFilters.Add(new ChipItem("Retard", AlertType.Retard, OnFilterSelected));
        TypeFilters.Add(new ChipItem("Incohérence", AlertType.Incoherence, OnFilterSelected));
    }

    private void OnFilterSelected(ChipItem chip)
    {
        foreach (var c in TypeFilters) c.IsSelected = (c == chip);
        _selectedTypeFilter = chip.Value as AlertType?;
        ApplyFilters();
    }

    public override async Task InitializeAsync()
    {
        await LoadAlertsAsync();
    }

    [RelayCommand]
    public async Task LoadAlertsAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _repository.GetAlertsAsync();
            _allAlerts = list.Select(a => new AlertItemVm(a)).ToList();

            UnreadCount = _allAlerts.Count(a => !a.IsRead);
            CriticalCount = _allAlerts.Count(a => a.Level == AlertLevel.Critical);

            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        FilteredAlerts.Clear();
        var q = _allAlerts.AsEnumerable();
        if (_selectedTypeFilter.HasValue)
        {
            q = q.Where(a => a.Type == _selectedTypeFilter.Value);
        }
        foreach (var a in q) FilteredAlerts.Add(a);
    }

    [RelayCommand]
    private async Task MarkAsReadAsync(AlertItemVm? item)
    {
        if (item == null) return;
        item.IsRead = true;
        await _repository.MarkAlertReadAsync(item.Alert.Id, true);
        UnreadCount = _allAlerts.Count(a => !a.IsRead);
    }

    [RelayCommand]
    private async Task MarkAllAsReadAsync()
    {
        await _repository.MarkAllAlertsReadAsync();
        foreach (var a in _allAlerts) a.IsRead = true;
        UnreadCount = 0;
        await DialogService.ShowToastAsync("Toutes les alertes sont marquées comme lues");
    }
}
