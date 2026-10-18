using ChantierPro.Mobile.ViewModels;

namespace ChantierPro.Mobile.Views;

public partial class AttendancePage : ContentPage
{
    private readonly AttendanceViewModel _viewModel;

    public AttendancePage(AttendanceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnOpenWorkersClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("workers");
    }
}
