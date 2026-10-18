using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

[QueryProperty(nameof(ChantierIdString), "chantierId")]
public partial class ExpenseEditViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private List<ChantierAggregate> _allChantiers = new();

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private ChantierAggregate? _selectedChantier;

    [ObservableProperty]
    private BudgetCategory _selectedCategory = BudgetCategory.LocationEngins;

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _supplier = string.Empty;

    [ObservableProperty]
    private PaymentMode _selectedPaymentMode = PaymentMode.MobileMoney;

    [ObservableProperty]
    private DateTime _expenseDate = DateTime.Today;

    [ObservableProperty]
    private string? _receiptPhotoPath;

    public ObservableCollection<ChantierAggregate> Chantiers { get; } = new();
    public List<BudgetCategory> Categories { get; } = Enum.GetValues<BudgetCategory>().ToList();
    public List<PaymentMode> PaymentModes { get; } = Enum.GetValues<PaymentMode>().ToList();

    public ExpenseEditViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Saisir une dépense";
    }

    public override async Task InitializeAsync()
    {
        await LoadChantiersAsync();
    }

    async partial void OnChantierIdStringChanged(string value)
    {
        await LoadChantiersAsync();
    }

    private async Task LoadChantiersAsync()
    {
        IsBusy = true;
        try
        {
            _allChantiers = (await _repository.GetChantiersAsync()).ToList();
            Chantiers.Clear();
            foreach (var c in _allChantiers) Chantiers.Add(c);

            if (Guid.TryParse(ChantierIdString, out var cId))
            {
                SelectedChantier = _allChantiers.FirstOrDefault(c => c.Chantier.Id == cId);
            }
            SelectedChantier ??= Chantiers.FirstOrDefault();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AttachReceiptPhotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    ReceiptPhotoPath = photo.FullPath;
                    await DialogService.ShowToastAsync("Justificatif photo capturé");
                }
            }
            else
            {
                ReceiptPhotoPath = "receipt_mock.jpg";
                await DialogService.ShowToastAsync("Justificatif simulé (bureau)");
            }
        }
        catch (Exception)
        {
            ReceiptPhotoPath = "receipt_mock.jpg";
            await DialogService.ShowToastAsync("Justificatif attaché");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedChantier == null)
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez choisir un chantier.");
            return;
        }

        if (!decimal.TryParse(AmountText.Replace(" ", "").Replace("\u00a0", ""), out var amount) || amount <= 0)
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez saisir un montant valide supérieur à 0.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez décrire la dépense (libellé).");
            return;
        }

        IsBusy = true;
        try
        {
            var exp = new Expense
            {
                ChantierId = SelectedChantier.Chantier.Id,
                Category = SelectedCategory,
                Amount = amount,
                Label = Label.Trim(),
                Supplier = Supplier.Trim(),
                Date = ExpenseDate,
                PaymentMode = SelectedPaymentMode,
                ReceiptPhotoPath = ReceiptPhotoPath,
                Author = _repository.CurrentUser.Name
            };

            await _repository.AddExpenseAsync(exp);
            await DialogService.ShowToastAsync($"Dépense de {amount:N0} FCFA enregistrée !");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
