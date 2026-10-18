using ChantierPro.Mobile.Helpers;

namespace ChantierPro.Mobile.Controls;

public partial class ProgressGauge : VerticalStackLayout
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(ProgressGauge), "Avancement",
            propertyChanged: (b, o, n) => ((ProgressGauge)b).lblTitle.Text = (string)n);

    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(ProgressGauge), 0.0,
            propertyChanged: (b, o, n) => ((ProgressGauge)b).UpdateProgress((double)n));

    public static readonly BindableProperty ExpectedProgressProperty =
        BindableProperty.Create(nameof(ExpectedProgress), typeof(double), typeof(ProgressGauge), 0.0,
            propertyChanged: (b, o, n) => ((ProgressGauge)b).UpdateExpected((double)n));

    public static readonly BindableProperty GaugeToneProperty =
        BindableProperty.Create(nameof(GaugeTone), typeof(Tone), typeof(ProgressGauge), Tone.Success,
            propertyChanged: (b, o, n) => ((ProgressGauge)b).UpdateTone((Tone)n));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double ExpectedProgress
    {
        get => (double)GetValue(ExpectedProgressProperty);
        set => SetValue(ExpectedProgressProperty, value);
    }

    public Tone GaugeTone
    {
        get => (Tone)GetValue(GaugeToneProperty);
        set => SetValue(GaugeToneProperty, value);
    }

    public ProgressGauge()
    {
        InitializeComponent();
        SizeChanged += (s, e) => UpdateBarWidth();
    }

    private void UpdateProgress(double value)
    {
        var clamped = Math.Max(0.0, Math.Min(100.0, value));
        lblPercent.Text = $"{clamped:0.#} %";
        UpdateBarWidth();
    }

    private void UpdateExpected(double expected)
    {
        if (expected > 0)
        {
            lblExpected.Text = $"Prévu : {expected:0.#} %";
        }
        else
        {
            lblExpected.Text = string.Empty;
        }
    }

    private void UpdateTone(Tone tone)
    {
        var color = Palette.Of(tone);
        barFill.BackgroundColor = color;
        badgeProgress.BackgroundColor = Palette.Tint(tone);
        lblPercent.TextColor = color;
    }

    private void UpdateBarWidth()
    {
        if (Width > 0)
        {
            var clamped = Math.Max(0.0, Math.Min(100.0, Progress));
            barFill.WidthRequest = (clamped / 100.0) * Width;
        }
    }
}
