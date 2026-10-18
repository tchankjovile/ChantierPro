using ChantierPro.Mobile.Helpers;

namespace ChantierPro.Mobile.Controls;

public partial class OfflineSyncBanner : Border
{
    public static readonly BindableProperty IsOnlineProperty =
        BindableProperty.Create(nameof(IsOnline), typeof(bool), typeof(OfflineSyncBanner), true,
            propertyChanged: (b, o, n) => ((OfflineSyncBanner)b).UpdateState((bool)n));

    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    public OfflineSyncBanner()
    {
        InitializeComponent();
        lblIcon.Text = Icons.CloudDone;
        btnSync.Clicked += async (s, e) =>
        {
            await btnSync.RotateTo(360, 600, Easing.CubicInOut);
            btnSync.Rotation = 0;
            lblStatus.Text = "Synchronisation terminée";
            lblDetail.Text = $"À l'instant ({DateTime.Now:HH:mm}) - 0 conflit";
        };
    }

    private void UpdateState(bool isOnline)
    {
        if (isOnline)
        {
            lblIcon.Text = Icons.CloudDone;
            lblIcon.TextColor = Palette.Success;
            lblStatus.Text = "Synchronisé avec le serveur";
            lblDetail.Text = "Dernière synchro réussie";
            BackgroundColor = Palette.Tint(Tone.Success);
        }
        else
        {
            lblIcon.Text = Icons.CloudOff;
            lblIcon.TextColor = Palette.Warning;
            lblStatus.Text = "Mode hors ligne actif";
            lblDetail.Text = "Saisies conservées en local, en attente de réseau";
            BackgroundColor = Palette.Tint(Tone.Warning);
        }
    }
}
