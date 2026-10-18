using ChantierPro.Mobile.ViewModels;

namespace ChantierPro.Mobile.Views;

public partial class PhaseDetailPage : ContentPage
{
    public PhaseDetailPage(PhaseDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnTaskCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        // La liaison bidirectionnelle gère la mise à jour de l'état IsDone
    }
}
