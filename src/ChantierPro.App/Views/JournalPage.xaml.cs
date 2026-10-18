using ChantierPro.Mobile.ViewModels;

namespace ChantierPro.Mobile.Views;

public partial class JournalPage : ContentPage
{
    private readonly JournalViewModel _viewModel;

    public JournalPage(JournalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
