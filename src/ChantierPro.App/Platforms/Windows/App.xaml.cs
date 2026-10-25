using Microsoft.UI.Xaml;
using ChantierPro.Mobile;

namespace ChantierPro.Mobile.WinUI;

/// <summary>Point d'entrée de la plateforme Windows (WinUI 3).</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
