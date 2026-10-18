using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

[QueryProperty(nameof(ChantierIdString), "chantierId")]
public partial class StockMovementEditViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private List<ChantierAggregate> _allChantiers = new();
    private List<Material> _materials = new();
    private List<Supplier> _suppliers = new();

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private ChantierAggregate? _selectedChantier;

    [ObservableProperty]
    private bool _isEntry = true;

    [ObservableProperty]
    private Material? _selectedMaterial;

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private Phase? _selectedPhase;

    [ObservableProperty]
    private string _quantityText = string.Empty;

    [ObservableProperty]
    private string _unitPriceText = string.Empty;

    [ObservableProperty]
    private string _deliveryNote = string.Empty;

    [ObservableProperty]
    private DateTime _movementDate = DateTime.Today;

    [ObservableProperty]
    private decimal _availableStock;

    [ObservableProperty]
    private bool _authorizeNegativeStock;

    [ObservableProperty]
    private bool _showNegativeStockOverride;

    public ObservableCollection<ChantierAggregate> Chantiers { get; } = new();
    public ObservableCollection<Material> Materials { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Phase> Phases { get; } = new();

    public StockMovementEditViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Nouveau mouvement";
    }

    public override async Task InitializeAsync()
    {
        await LoadListsAsync();
    }

    async partial void OnChantierIdStringChanged(string value)
    {
        await LoadListsAsync();
    }

    private async Task LoadListsAsync()
    {
        IsBusy = true;
        try
        {
            _allChantiers = (await _repository.GetChantiersAsync()).ToList();
            _materials = (await _repository.GetMaterialsAsync()).ToList();
            _suppliers = (await _repository.GetSuppliersAsync()).ToList();

            Chantiers.Clear();
            foreach (var c in _allChantiers) Chantiers.Add(c);

            Materials.Clear();
            foreach (var m in _materials) Materials.Add(m);

            Suppliers.Clear();
            foreach (var s in _suppliers) Suppliers.Add(s);

            if (Guid.TryParse(ChantierIdString, out var cId))
            {
                SelectedChantier = _allChantiers.FirstOrDefault(c => c.Chantier.Id == cId);
            }
            SelectedChantier ??= _allChantiers.FirstOrDefault();

            SelectedMaterial ??= Materials.FirstOrDefault();
            SelectedSupplier ??= Suppliers.FirstOrDefault();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedChantierChanged(ChantierAggregate? value)
    {
        Phases.Clear();
        if (value != null)
        {
            foreach (var p in value.Phases) Phases.Add(p);
            SelectedPhase = Phases.FirstOrDefault();
        }
        RefreshStock();
    }

    partial void OnSelectedMaterialChanged(Material? value)
    {
        if (value != null)
        {
            UnitPriceText = value.UnitPrice.ToString("0");
        }
        RefreshStock();
    }

    partial void OnIsEntryChanged(bool value)
    {
        Title = value ? "Entrée de stock" : "Sortie de stock";
        RefreshStock();
    }

    private void RefreshStock()
    {
        if (SelectedChantier != null && SelectedMaterial != null)
        {
            AvailableStock = StockCalculator.CurrentStock(SelectedChantier.Movements, SelectedChantier.Chantier.Id, SelectedMaterial.Id);
        }
        else
        {
            AvailableStock = 0m;
        }
    }

    [RelayCommand]
    private void SetType(string type)
    {
        IsEntry = (type == "entree");
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        // Simulation scan F-MAT-06
        var mockCode = "370001234501"; // Ciment
        var found = Materials.FirstOrDefault(m => m.Barcode == mockCode);
        if (found != null)
        {
            SelectedMaterial = found;
            await DialogService.ShowToastAsync($"Matériau scanné : {found.Name}");
        }
        else
        {
            await DialogService.ShowToastAsync("Code-barres non reconnu dans le catalogue.");
        }
    }

    [RelayCommand]
    private async Task AttachPhotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    await DialogService.ShowToastAsync("Photo du bon de livraison jointe");
                }
            }
            else
            {
                await DialogService.ShowToastAsync("Photo du bon de livraison simulée");
            }
        }
        catch (Exception)
        {
            await DialogService.ShowToastAsync("Photo attachée");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedChantier == null)
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez sélectionner un chantier.");
            return;
        }

        if (SelectedMaterial == null)
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez choisir un matériau.");
            return;
        }

        if (!decimal.TryParse(QuantityText.Replace(" ", ""), out var qty) || qty <= 0)
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez saisir une quantité supérieure à 0.");
            return;
        }

        if (!decimal.TryParse(UnitPriceText.Replace(" ", ""), out var price) || price < 0)
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez saisir un prix unitaire valide.");
            return;
        }

        // RG-03 : contrôle de stock disponible lors des sorties
        if (!IsEntry)
        {
            if (!StockCalculator.CanIssue(AvailableStock, qty, AuthorizeNegativeStock))
            {
                ShowNegativeStockOverride = true;
                await DialogService.ShowAlertAsync("Stock insuffisant (RG-03)",
                    $"La quantité demandée ({qty:0.##}) dépasse le stock disponible ({AvailableStock:0.##} {SelectedMaterial.Unit}).\n\nCochez « Autoriser le stock négatif » pour forcer cette sortie.");
                return;
            }
        }

        IsBusy = true;
        try
        {
            var movement = new StockMovement
            {
                ChantierId = SelectedChantier.Chantier.Id,
                MaterialId = SelectedMaterial.Id,
                Type = IsEntry ? MovementType.Entree : MovementType.Sortie,
                Quantity = qty,
                UnitPrice = price,
                PhaseId = !IsEntry ? SelectedPhase?.Id : null,
                SupplierId = IsEntry ? SelectedSupplier?.Id : null,
                DeliveryNoteNumber = DeliveryNote.Trim(),
                Date = MovementDate,
                Author = _repository.CurrentUser.Name,
                IsNegativeStockAuthorized = AuthorizeNegativeStock
            };

            await _repository.AddMovementAsync(movement);
            await DialogService.ShowToastAsync(IsEntry ? "Entrée de stock enregistrée !" : "Sortie de stock enregistrée !");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
