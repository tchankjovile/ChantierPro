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

public class StockItemVm
{
    public Material Material { get; }
    public decimal CurrentStock { get; }
    public decimal AlertThreshold => Material.AlertThreshold;
    public bool IsLow => CurrentStock <= AlertThreshold;
    public decimal UnitPrice => Material.UnitPrice;
    public decimal StockValue => CurrentStock * UnitPrice;
    public decimal? DaysRemaining { get; }

    public string Name => Material.Name;
    public string Category => Material.Category;
    public string Unit => Material.Unit;
    public Tone StockTone => CurrentStock <= 0 ? Tone.Danger : (IsLow ? Tone.Warning : Tone.Success);

    public StockItemVm(Material material, decimal currentStock, decimal? daysRemaining)
    {
        Material = material;
        CurrentStock = currentStock;
        DaysRemaining = daysRemaining;
    }
}

public class MovementItemVm
{
    public StockMovement Movement { get; }
    public string MaterialName { get; }
    public string Unit { get; }
    public string SupplierOrPhase { get; }

    public MovementType Type => Movement.Type;
    public decimal Quantity => Movement.Quantity;
    public decimal UnitPrice => Movement.UnitPrice;
    public decimal TotalCost => Movement.TotalCost;
    public DateTime Date => Movement.Date;
    public string DeliveryNote => Movement.DeliveryNoteNumber ?? "—";
    public string Subtitle => $"{Date:dd/MM/yyyy} • Bon : {DeliveryNote}";
    public Tone TypeTone => Movement.Type == MovementType.Entree ? Tone.Success : Tone.Info;
    public string TypeLabel => Movement.Type == MovementType.Entree ? "Entrée" : "Sortie";

    public MovementItemVm(StockMovement movement, string materialName, string unit, string supplierOrPhase)
    {
        Movement = movement;
        MaterialName = materialName;
        Unit = unit;
        SupplierOrPhase = supplierOrPhase;
    }
}

[QueryProperty(nameof(ChantierIdString), "id")]
public partial class StockViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private ChantierAggregate? _selectedChantier;
    private List<ChantierAggregate> _allChantiers = new();
    private List<Material> _materials = new();
    private List<MovementItemVm> _allMovements = new();

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private string _chantierName = "Tous les chantiers";

    [ObservableProperty]
    private decimal _totalStockValue;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _criticalStockCount;

    [ObservableProperty]
    private bool _showStockTab = true;

    public ObservableCollection<StockItemVm> StockItems { get; } = new();
    public ObservableCollection<MovementItemVm> FilteredMovements { get; } = new();
    public ObservableCollection<ChipItem> MovementFilters { get; } = new();

    private MovementType? _selectedMovementFilter = null;

    public StockViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Stock & Matériaux";
        InitFilters();
    }

    private void InitFilters()
    {
        MovementFilters.Add(new ChipItem("Tous", null, OnMovementFilterSelected, isSelected: true));
        MovementFilters.Add(new ChipItem("Entrées", MovementType.Entree, OnMovementFilterSelected));
        MovementFilters.Add(new ChipItem("Sorties", MovementType.Sortie, OnMovementFilterSelected));
    }

    private void OnMovementFilterSelected(ChipItem chip)
    {
        foreach (var c in MovementFilters) c.IsSelected = (c == chip);
        _selectedMovementFilter = chip.Value as MovementType?;
        ApplyMovementFilters();
    }

    public override async Task InitializeAsync()
    {
        await LoadAsync();
    }

    async partial void OnChantierIdStringChanged(string value)
    {
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _allChantiers = (await _repository.GetChantiersAsync()).ToList();
            _materials = (await _repository.GetMaterialsAsync()).ToList();

            if (Guid.TryParse(ChantierIdString, out var cId))
            {
                _selectedChantier = _allChantiers.FirstOrDefault(c => c.Chantier.Id == cId);
            }
            if (_selectedChantier == null && _allChantiers.Count > 0)
            {
                _selectedChantier = _allChantiers.First();
            }

            if (_selectedChantier == null) return;
            ChantierName = _selectedChantier.Chantier.Name;

            var movements = _selectedChantier.Movements;
            var suppliers = await _repository.GetSuppliersAsync();
            var supDict = suppliers.ToDictionary(s => s.Id, s => s.Name);
            var phaseDict = _selectedChantier.Phases.ToDictionary(p => p.Id, p => p.Name);
            var matDict = _materials.ToDictionary(m => m.Id, m => m);

            StockItems.Clear();
            decimal totalVal = 0m;
            int lowCount = 0;
            int critCount = 0;

            foreach (var mat in _materials)
            {
                var cur = StockCalculator.CurrentStock(movements, _selectedChantier.Chantier.Id, mat.Id);
                var days = StockCalculator.DaysRemaining(cur, movements, _selectedChantier.Chantier.Id, mat.Id, DateTime.Today);
                var item = new StockItemVm(mat, cur, days);
                StockItems.Add(item);

                totalVal += item.StockValue;
                if (cur <= 0) critCount++;
                else if (item.IsLow) lowCount++;
            }

            TotalStockValue = totalVal;
            LowStockCount = lowCount;
            CriticalStockCount = critCount;

            _allMovements.Clear();
            foreach (var m in movements.OrderByDescending(x => x.Date))
            {
                var matName = matDict.TryGetValue(m.MaterialId, out var mat) ? mat.Name : "Matériau";
                var unit = mat?.Unit ?? "u";
                string dest = "—";
                if (m.Type == MovementType.Entree && m.SupplierId.HasValue && supDict.TryGetValue(m.SupplierId.Value, out var sName))
                    dest = sName;
                else if (m.Type == MovementType.Sortie && m.PhaseId.HasValue && phaseDict.TryGetValue(m.PhaseId.Value, out var pName))
                    dest = pName;

                _allMovements.Add(new MovementItemVm(m, matName, unit, dest));
            }

            ApplyMovementFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyMovementFilters()
    {
        FilteredMovements.Clear();
        var q = _allMovements.AsEnumerable();
        if (_selectedMovementFilter.HasValue)
        {
            q = q.Where(m => m.Type == _selectedMovementFilter.Value);
        }
        foreach (var m in q)
        {
            FilteredMovements.Add(m);
        }
    }

    [RelayCommand]
    private void SwitchTab(string tab)
    {
        ShowStockTab = (tab == "stock");
    }

    [RelayCommand]
    private async Task AddMovementAsync()
    {
        var id = _selectedChantier?.Chantier.Id.ToString() ?? string.Empty;
        await Shell.Current.GoToAsync($"stock-movement-edit?chantierId={id}");
    }

    [RelayCommand]
    private async Task SelectChantierAsync()
    {
        if (_allChantiers.Count <= 1) return;
        var names = _allChantiers.Select(c => c.Chantier.Name).ToArray();
        var selected = await DialogService.ShowActionSheetAsync("Choisir le chantier", "Annuler", null, names);
        if (selected != null && selected != "Annuler")
        {
            _selectedChantier = _allChantiers.FirstOrDefault(c => c.Chantier.Name == selected);
            if (_selectedChantier != null)
            {
                ChantierIdString = _selectedChantier.Chantier.Id.ToString();
                await LoadAsync();
            }
        }
    }
}
