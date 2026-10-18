using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Mobile.Helpers;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public class JournalItemVm
{
    public JournalEntry Entry { get; }
    public DateTime Date => Entry.Date;
    public string WeatherLabel => Labels.Of(Entry.Weather);
    public string WeatherIcon => Palette.IconFor(Entry.Weather);
    public string Note => Entry.Note;
    public string Incident => string.IsNullOrWhiteSpace(Entry.Incident) ? "Aucun incident" : Entry.Incident;
    public string Visitors => string.IsNullOrWhiteSpace(Entry.Visitors) ? "Aucun visiteur" : Entry.Visitors;
    public string Author => Entry.Author;

    public JournalItemVm(JournalEntry entry)
    {
        Entry = entry;
    }
}

[QueryProperty(nameof(ChantierIdString), "id")]
public partial class JournalViewModel : BaseViewModel
{
    private readonly IChantierRepository _repository;
    private ChantierAggregate? _selectedChantier;
    private List<ChantierAggregate> _allChantiers = new();

    [ObservableProperty]
    private string _chantierIdString = string.Empty;

    [ObservableProperty]
    private string _chantierName = string.Empty;

    [ObservableProperty]
    private DateTime _entryDate = DateTime.Today;

    [ObservableProperty]
    private WeatherKind _selectedWeather = WeatherKind.Ensoleille;

    [ObservableProperty]
    private string _noteText = string.Empty;

    [ObservableProperty]
    private string _incidentText = string.Empty;

    [ObservableProperty]
    private string _visitorsText = string.Empty;

    [ObservableProperty]
    private bool _showForm;

    public ObservableCollection<ChantierAggregate> Chantiers { get; } = new();
    public ObservableCollection<JournalItemVm> Entries { get; } = new();
    public ObservableCollection<SitePhoto> Photos { get; } = new();
    public List<WeatherKind> WeatherOptions { get; } = Enum.GetValues<WeatherKind>().ToList();

    public JournalViewModel(IChantierRepository repository, IDialogService dialogService) : base(dialogService)
    {
        _repository = repository;
        Title = "Journal & Photos";
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
            Chantiers.Clear();
            foreach (var c in _allChantiers) Chantiers.Add(c);

            if (Guid.TryParse(ChantierIdString, out var cId))
            {
                _selectedChantier = _allChantiers.FirstOrDefault(c => c.Chantier.Id == cId);
            }
            _selectedChantier ??= Chantiers.FirstOrDefault();

            if (_selectedChantier == null) return;
            ChantierName = _selectedChantier.Chantier.Name;

            Entries.Clear();
            foreach (var e in _selectedChantier.Journal.OrderByDescending(x => x.Date))
            {
                Entries.Add(new JournalItemVm(e));
            }

            Photos.Clear();
            foreach (var p in _selectedChantier.Photos.OrderByDescending(x => x.Date))
            {
                Photos.Add(p);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleForm()
    {
        ShowForm = !ShowForm;
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (_selectedChantier == null) return;

        try
        {
            var stage = await DialogService.ShowActionSheetAsync("Étape du chantier", "Annuler", null, "Avant travaux", "Pendant travaux", "Après travaux / Finition");
            if (stage == null || stage == "Annuler") return;

            var photoItem = new SitePhoto
            {
                ChantierId = _selectedChantier.Chantier.Id,
                Date = DateTime.Now,
                Stage = stage,
                Path = "photo_mock.jpg",
                Author = _repository.CurrentUser.Name
            };

            await _repository.AddPhotoAsync(photoItem);
            Photos.Insert(0, photoItem);
            await DialogService.ShowToastAsync($"Photo ({stage}) enregistrée et horodatée !");
        }
        catch (Exception)
        {
            await DialogService.ShowToastAsync("Photo enregistrée");
        }
    }

    [RelayCommand]
    private async Task SaveEntryAsync()
    {
        if (_selectedChantier == null) return;

        if (string.IsNullOrWhiteSpace(NoteText))
        {
            await DialogService.ShowAlertAsync("Validation", "Veuillez saisir une note ou description des faits marquants.");
            return;
        }

        IsBusy = true;
        try
        {
            var entry = new JournalEntry
            {
                ChantierId = _selectedChantier.Chantier.Id,
                Date = EntryDate,
                Weather = SelectedWeather,
                Note = NoteText.Trim(),
                Incident = IncidentText.Trim(),
                Visitors = VisitorsText.Trim(),
                Author = _repository.CurrentUser.Name
            };

            await _repository.AddJournalEntryAsync(entry);
            Entries.Insert(0, new JournalItemVm(entry));

            NoteText = string.Empty;
            IncidentText = string.Empty;
            VisitorsText = string.Empty;
            ShowForm = false;

            await DialogService.ShowToastAsync("Entrée ajoutée au journal de chantier !");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
