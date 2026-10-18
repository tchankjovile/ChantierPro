using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public partial class WorkersViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<Worker> Workers { get; } = new();
    private List<Worker> _allWorkers = new();

    public WorkersViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Annuaire des ouvriers";
    }

    public override async Task InitializeAsync()
    {
        await LoadWorkersAsync();
    }

    [RelayCommand]
    public async Task LoadWorkersAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _repository.GetWorkersAsync();
            _allWorkers = list.ToList();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Workers.Clear();
        var q = _allWorkers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim().ToLowerInvariant();
            q = q.Where(w => w.Name.ToLowerInvariant().Contains(term) || w.Trade.ToLowerInvariant().Contains(term));
        }
        foreach (var w in q) Workers.Add(w);
    }

    [RelayCommand]
    private async Task AddWorkerAsync()
    {
        var name = await DialogService.ShowPromptAsync("Nouvel ouvrier", "Nom complet de l'ouvrier :");
        if (string.IsNullOrWhiteSpace(name)) return;

        var trade = await DialogService.ShowPromptAsync("Métier", "Corps d'état / métier (ex: Maçon, Ferrailleur, Plombier) :");
        if (string.IsNullOrWhiteSpace(trade)) return;

        var rateStr = await DialogService.ShowPromptAsync("Taux journalier", "Taux journalier en FCFA (ex: 10000) :", initialValue: "10000");
        if (!decimal.TryParse(rateStr, out var rate)) rate = 10000m;

        var phone = await DialogService.ShowPromptAsync("Téléphone", "Numéro de téléphone :", initialValue: "+237 6");

        var w = new Worker
        {
            Name = name.Trim(),
            Trade = trade.Trim(),
            DailyRate = rate,
            Phone = phone?.Trim() ?? string.Empty,
            Status = WorkerStatus.Actif
        };

        await _repository.SaveWorkerAsync(w);
        await LoadWorkersAsync();
        await DialogService.ShowToastAsync($"Ouvrier ajouté : {w.Name}");
    }

    [RelayCommand]
    private async Task CallWorkerAsync(Worker? worker)
    {
        if (worker == null || string.IsNullOrWhiteSpace(worker.Phone)) return;
        try
        {
            PhoneDialer.Default.Open(worker.Phone);
        }
        catch (Exception)
        {
            await DialogService.ShowAlertAsync("Appel", $"Composition vers : {worker.Phone}");
        }
    }
}
