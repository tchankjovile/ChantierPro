using CommunityToolkit.Mvvm.ComponentModel;
using ChantierPro.Mobile.Services;

namespace ChantierPro.Mobile.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    protected readonly IDialogService DialogService;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    protected BaseViewModel(IDialogService dialogService)
    {
        DialogService = dialogService;
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
}
