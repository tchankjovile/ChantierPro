using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Helpers;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public class BudgetLineItemVm
{
    public BudgetLineResult Line { get; }
    public string CategoryLabel => Labels.Of(Line.Category);
    public string CategoryIcon => Palette.IconFor(Line.Category);
    public decimal Planned => Line.Planned;
    public decimal Actual => Line.Actual;
    public decimal Variance => Line.Variance;
    public decimal VariancePercent => Line.VariancePercent;
    public double RatePercent => (double)(Line.Rate * 100m);
    public Tone Tone => Line.Level.HasValue ? Palette.ToneFor(Line.Level.Value) : (Line.Rate >= 0.8m ? Tone.Warning : Tone.Success);
    public bool IsOverBudget => Line.Actual > Line.Planned && Line.Planned > 0;

    public BudgetLineItemVm(BudgetLineResult line)
    {
        Line = line;
    }
}

public class ExpenseItemVm
{
    public Expense Expense { get; }
    public string CategoryLabel => Labels.Of(Expense.Category);
    public string CategoryIcon => Palette.IconFor(Expense.Category);
    public string Supplier => string.IsNullOrWhiteSpace(Expense.Supplier) ? "Fournisseur direct" : Expense.Supplier;
    public string Label => Expense.Label;
    public decimal Amount => Expense.Amount;
    public DateTime Date => Expense.Date;
    public string PaymentMode => Labels.Of(Expense.PaymentMode);

    public ExpenseItemVm(Expense expense)
    {
        Expense = expense;
    }
}

[QueryProperty(nameof(ChantierIdString), "id")]
public partial class BudgetViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private ChantierAggregate? _selectedChantier;
    private List<ChantierAggregate> _allChantiers = new();

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private string _chantierName = string.Empty;

    [ObservableProperty]
    private decimal _totalBudget;

    [ObservableProperty]
    private decimal _totalConsumed;

    [ObservableProperty]
    private decimal _totalRemaining;

    [ObservableProperty]
    private double _consumptionRate;

    [ObservableProperty]
    private Tone _budgetTone = Tone.Success;

    [ObservableProperty]
    private decimal _estimateAtCompletion;

    [ObservableProperty]
    private decimal _projectedOverrun;

    [ObservableProperty]
    private decimal _expensesShare;

    [ObservableProperty]
    private decimal _materialsShare;

    [ObservableProperty]
    private decimal _labourShare;

    [ObservableProperty]
    private bool _showLinesTab = true;

    public ObservableCollection<BudgetLineItemVm> Lines { get; } = new();
    public ObservableCollection<ExpenseItemVm> RecentExpenses { get; } = new();

    public BudgetViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Budget & Dépenses";
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

            var materials = await _repository.GetMaterialsAsync();
            var insight = ChantierInsightService.Analyze(_selectedChantier, materials, DateTime.Today);
            var metrics = insight.Metrics;

            TotalBudget = _selectedChantier.Chantier.TotalBudget;
            TotalConsumed = metrics.Consumed;
            TotalRemaining = metrics.Remaining;
            ConsumptionRate = (double)(metrics.ConsumptionRate * 100m);
            BudgetTone = Palette.ToneForRate(metrics.ConsumptionRate);
            EstimateAtCompletion = metrics.EstimateAtCompletion;
            ProjectedOverrun = metrics.ProjectedOverrun(TotalBudget);

            ExpensesShare = metrics.ExpensesTotal;
            MaterialsShare = metrics.MaterialsTotal;
            LabourShare = metrics.LabourTotal;

            Lines.Clear();
            foreach (var l in metrics.Lines)
            {
                Lines.Add(new BudgetLineItemVm(l));
            }

            RecentExpenses.Clear();
            foreach (var e in _selectedChantier.Expenses.OrderByDescending(x => x.Date))
            {
                RecentExpenses.Add(new ExpenseItemVm(e));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SwitchTab(string tab)
    {
        ShowLinesTab = (tab == "lines");
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        var id = _selectedChantier?.Chantier.Id.ToString() ?? string.Empty;
        await Shell.Current.GoToAsync($"expense-edit?chantierId={id}");
    }
}
