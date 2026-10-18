using ChantierPro.Mobile.ViewModels;

namespace ChantierPro.Mobile.Views;

public partial class StockMovementEditPage : ContentPage
{
    private readonly StockMovementEditViewModel _viewModel;

    public StockMovementEditPage(StockMovementEditViewModel viewModel)
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
