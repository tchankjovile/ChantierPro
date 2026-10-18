using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Helpers;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public partial class AttendanceRowVm : ObservableObject
{
    public Worker Worker { get; }
    public string Name => Worker.Name;
    public string Trade => Worker.Trade;
    public decimal DailyRate => Worker.DailyRate;

    [ObservableProperty]
    private AttendanceStatus _status = AttendanceStatus.Present;

    [ObservableProperty]
    private decimal _overtimeHours = 0m;

    [ObservableProperty]
    private decimal _cost;

    public Tone StatusTone => Status switch
    {
        AttendanceStatus.Present => Tone.Success,
        AttendanceStatus.Retard => Tone.Warning,
        AttendanceStatus.DemiJournee => Tone.Info,
        _ => Tone.Danger
    };

    public AttendanceRowVm(Worker worker, Attendance? existing = null)
    {
        Worker = worker;
        if (existing != null)
        {
            _status = existing.Status;
            _overtimeHours = existing.OvertimeHours;
        }
        Recalculate();
    }

    partial void OnStatusChanged(AttendanceStatus value) => Recalculate();
    partial void OnOvertimeHoursChanged(decimal value) => Recalculate();

    public void Recalculate()
    {
        Cost = LabourCostCalculator.DayCost(DailyRate, Status, OvertimeHours);
        OnPropertyChanged(nameof(StatusTone));
    }
}

public partial class AttendanceViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private List<ChantierAggregate> _allChantiers = new();

    [ObservableProperty]
    private ChantierAggregate? _selectedChantier;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private int _presentCount;

    [ObservableProperty]
    private int _absentCount;

    [ObservableProperty]
    private decimal _totalDayLabourCost;

    [ObservableProperty]
    private decimal _totalOvertimeHours;

    public ObservableCollection<ChantierAggregate> Chantiers { get; } = new();
    public ObservableCollection<AttendanceRowVm> Rows { get; } = new();

    public AttendanceViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Pointage quotidien";
    }

    public override async Task InitializeAsync()
    {
        await LoadAsync();
    }

    partial void OnSelectedChantierChanged(ChantierAggregate? value) => _ = LoadRowsAsync();
    partial void OnSelectedDateChanged(DateTime value) => _ = LoadRowsAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _allChantiers = (await _repository.GetChantiersAsync()).ToList();
            Chantiers.Clear();
            foreach (var c in _allChantiers) Chantiers.Add(c);

            SelectedChantier ??= Chantiers.FirstOrDefault();
            await LoadRowsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadRowsAsync()
    {
        if (SelectedChantier == null) return;

        var workers = await _repository.GetWorkersAsync();
        var existingAttendances = SelectedChantier.Attendances
            .Where(a => a.Date.Date == SelectedDate.Date)
            .ToDictionary(a => a.WorkerId, a => a);

        Rows.Clear();
        foreach (var w in workers.Where(w => w.Status == WorkerStatus.Actif))
        {
            existingAttendances.TryGetValue(w.Id, out var att);
            var row = new AttendanceRowVm(w, att);
            row.PropertyChanged += (s, e) => UpdateTotals();
            Rows.Add(row);
        }

        UpdateTotals();
    }

    private void UpdateTotals()
    {
        PresentCount = Rows.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Retard);
        AbsentCount = Rows.Count(r => r.Status == AttendanceStatus.Absent);
        TotalDayLabourCost = Rows.Sum(r => r.Cost);
        TotalOvertimeHours = Rows.Sum(r => r.OvertimeHours);
    }

    [RelayCommand]
    private void MarkAllPresent()
    {
        foreach (var r in Rows)
        {
            r.Status = AttendanceStatus.Present;
        }
        UpdateTotals();
    }

    [RelayCommand]
    private void MarkAllAbsent()
    {
        foreach (var r in Rows)
        {
            r.Status = AttendanceStatus.Absent;
            r.OvertimeHours = 0m;
        }
        UpdateTotals();
    }

    [RelayCommand]
    private void CycleStatus(AttendanceRowVm? row)
    {
        if (row == null) return;
        row.Status = row.Status switch
        {
            AttendanceStatus.Present => AttendanceStatus.Retard,
            AttendanceStatus.Retard => AttendanceStatus.DemiJournee,
            AttendanceStatus.DemiJournee => AttendanceStatus.Absent,
            _ => AttendanceStatus.Present
        };
    }

    [RelayCommand]
    private void AddOvertime(AttendanceRowVm? row)
    {
        if (row == null || row.Status == AttendanceStatus.Absent) return;
        row.OvertimeHours += 1m;
        if (row.OvertimeHours > 6m) row.OvertimeHours = 0m;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedChantier == null) return;

        IsBusy = true;
        try
        {
            var list = Rows.Select(r => new Attendance
            {
                ChantierId = SelectedChantier.Chantier.Id,
                WorkerId = r.Worker.Id,
                Date = SelectedDate,
                Status = r.Status,
                DailyRate = r.DailyRate,
                OvertimeHours = r.OvertimeHours
            }).ToList();

            await _repository.SaveAttendancesAsync(SelectedChantier.Chantier.Id, SelectedDate, list);
            await DialogService.ShowToastAsync($"Pointage du {SelectedDate:dd/MM/yyyy} validé ({PresentCount} présents, {TotalDayLabourCost:N0} FCFA)");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
