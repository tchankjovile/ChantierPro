using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Helpers;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public class PhaseItemVm : ObservableObject
{
    public Phase Phase { get; }
    public string Name => Phase.Name;
    public int Order => Phase.Order;
    public decimal Weight => Phase.Weight;
    public decimal AllocatedBudget => Phase.AllocatedBudget;
    public double Progress => (double)Phase.Progress;
    public double ExpectedProgress => (double)ProgressCalculator.ExpectedPhaseProgress(Phase, DateTime.Today);
    public bool IsLate => ProgressCalculator.IsPhaseLate(Phase, DateTime.Today);
    public Tone StatusTone => IsLate ? Tone.Warning : (Phase.Progress >= 100m ? Tone.Success : Tone.Info);
    public string DateRangeText => $"{Phase.StartDate:dd MMM} - {Phase.EndDate:dd MMM yyyy}";
    public int TasksDone => Phase.Tasks.Count(t => t.IsDone);
    public int TasksTotal => Phase.Tasks.Count;

    public PhaseItemVm(Phase phase)
    {
        Phase = phase;
    }
}

[QueryProperty(nameof(ChantierIdString), "id")]
public partial class PhasesViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private ChantierAggregate? _aggregate;

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private string _chantierName = string.Empty;

    [ObservableProperty]
    private double _globalProgress;

    [ObservableProperty]
    private double _expectedGlobalProgress;

    [ObservableProperty]
    private bool _areWeightsValid;

    [ObservableProperty]
    private int _latePhasesCount;

    public ObservableCollection<PhaseItemVm> Phases { get; } = new();

    public PhasesViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Phases & Avancement";
    }

    async partial void OnChantierIdStringChanged(string value)
    {
        if (Guid.TryParse(value, out var id))
        {
            await LoadAsync(id);
        }
    }

    public async Task LoadAsync(Guid id)
    {
        IsBusy = true;
        try
        {
            _aggregate = await _repository.GetChantierAsync(id);
            if (_aggregate == null) return;

            ChantierName = _aggregate.Chantier.Name;
            GlobalProgress = (double)ProgressCalculator.GlobalProgress(_aggregate.Phases);
            ExpectedGlobalProgress = (double)ProgressCalculator.ExpectedGlobalProgress(_aggregate.Phases, DateTime.Today);
            AreWeightsValid = ProgressCalculator.AreWeightsValid(_aggregate.Phases);
            LatePhasesCount = _aggregate.Phases.Count(p => ProgressCalculator.IsPhaseLate(p, DateTime.Today));

            Phases.Clear();
            foreach (var p in _aggregate.Phases.OrderBy(x => x.Order))
            {
                Phases.Add(new PhaseItemVm(p));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenPhaseDetailAsync(PhaseItemVm? item)
    {
        if (item == null || _aggregate == null) return;
        await Shell.Current.GoToAsync($"phase-detail?chantierId={_aggregate.Chantier.Id}&phaseId={item.Phase.Id}");
    }
}
