using ChantierPro.Mobile.ViewModels;

namespace ChantierPro.Mobile.Views;

public partial class ChantierEditPage : ContentPage
{
    public ChantierEditPage(ChantierEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
