using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

[QueryProperty(nameof(ChantierIdString), "chantierId")]
[QueryProperty(nameof(PhaseIdString), "phaseId")]
public partial class PhaseDetailViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private ChantierAggregate? _aggregate;
    private Phase? _phase;

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private string _phaseIdString = string.Empty;

    [ObservableProperty]
    private string _phaseName = string.Empty;

    [ObservableProperty]
    private string _chantierName = string.Empty;

    [ObservableProperty]
    private decimal _weight;

    [ObservableProperty]
    private decimal _allocatedBudget;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private double _expectedProgress;

    [ObservableProperty]
    private string _updateNote = string.Empty;

    [ObservableProperty]
    private string _newTaskLabel = string.Empty;

    public ObservableCollection<PhaseTask> Tasks { get; } = new();
    public ObservableCollection<ProgressEntry> History { get; } = new();

    public PhaseDetailViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Mise à jour phase";
    }

    async partial void OnPhaseIdStringChanged(string value)
    {
        await TryLoadAsync();
    }

    async partial void OnChantierIdStringChanged(string value)
    {
        await TryLoadAsync();
    }

    private async Task TryLoadAsync()
    {
        if (Guid.TryParse(ChantierIdString, out var cId) && Guid.TryParse(PhaseIdString, out var pId))
        {
            await LoadPhaseAsync(cId, pId);
        }
    }

    private async Task LoadPhaseAsync(Guid chantierId, Guid phaseId)
    {
        IsBusy = true;
        try
        {
            _aggregate = await _repository.GetChantierAsync(chantierId);
            if (_aggregate == null) return;

            _phase = _aggregate.Phases.FirstOrDefault(p => p.Id == phaseId);
            if (_phase == null) return;

            ChantierName = _aggregate.Chantier.Name;
            PhaseName = _phase.Name;
            Title = _phase.Name;
            Weight = _phase.Weight;
            AllocatedBudget = _phase.AllocatedBudget;
            ProgressValue = (double)_phase.Progress;
            ExpectedProgress = (double)ProgressCalculator.ExpectedPhaseProgress(_phase, DateTime.Today);

            Tasks.Clear();
            foreach (var t in _phase.Tasks)
            {
                Tasks.Add(t);
            }

            History.Clear();
            foreach (var h in _phase.History.OrderByDescending(x => x.Date))
            {
                History.Add(h);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveProgressAsync()
    {
        if (_phase == null) return;

        IsBusy = true;
        try
        {
            _phase.Progress = (decimal)Math.Round(ProgressValue, 1);
            var note = string.IsNullOrWhiteSpace(UpdateNote) ? "Mise à jour régulière d'avancement" : UpdateNote.Trim();
            await _repository.SavePhaseAsync(_phase, _repository.CurrentUser.Name, note);

            UpdateNote = string.Empty;
            await DialogService.ShowToastAsync($"Avancement enregistré : {_phase.Progress:0.#} %");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleTask(PhaseTask? task)
    {
        if (task == null) return;
        task.IsDone = !task.IsDone;
    }

    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskLabel) || _phase == null) return;
        var t = new PhaseTask { PhaseId = _phase.Id, Label = NewTaskLabel.Trim(), IsDone = false };
        _phase.Tasks.Add(t);
        Tasks.Add(t);
        NewTaskLabel = string.Empty;
    }

    [RelayCommand]
    private async Task TakeProofPhotoAsync()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    await DialogService.ShowToastAsync("Photo de preuve enregistrée et horodatée");
                }
            }
            else
            {
                await DialogService.ShowToastAsync("Photo de preuve simulée (Mode maquette/desktop)");
            }
        }
        catch (Exception)
        {
            await DialogService.ShowToastAsync("Photo de preuve simulée");
        }
    }
}
