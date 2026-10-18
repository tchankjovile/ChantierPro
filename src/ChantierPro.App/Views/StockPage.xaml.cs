using ChantierPro.Mobile.ViewModels;

namespace ChantierPro.Mobile.Views;

public partial class StockPage : ContentPage
{
    private readonly StockViewModel _viewModel;

    public StockPage(StockViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
