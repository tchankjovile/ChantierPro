using ChantierPro.Mobile.Helpers;

namespace ChantierPro.Mobile.Controls;

public partial class StatCard : Border
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(StatCard), string.Empty,
            propertyChanged: (b, o, n) => ((StatCard)b).lblCaption.Text = (string)n);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(StatCard), string.Empty,
            propertyChanged: (b, o, n) => ((StatCard)b).lblValue.Text = (string)n);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(StatCard), string.Empty,
            propertyChanged: (b, o, n) => ((StatCard)b).lblSubtitle.Text = (string)n);

    public static readonly BindableProperty IconGlyphProperty =
        BindableProperty.Create(nameof(IconGlyph), typeof(string), typeof(StatCard), string.Empty,
            propertyChanged: (b, o, n) => ((StatCard)b).lblIcon.Text = (string)n);

    public static readonly BindableProperty CardToneProperty =
        BindableProperty.Create(nameof(CardTone), typeof(Tone), typeof(StatCard), Tone.Neutral,
            propertyChanged: (b, o, n) => ((StatCard)b).UpdateTone((Tone)n));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public Tone CardTone
    {
        get => (Tone)GetValue(CardToneProperty);
        set => SetValue(CardToneProperty, value);
    }

    public StatCard()
    {
        InitializeComponent();
    }

    private void UpdateTone(Tone tone)
    {
        var color = Palette.Of(tone);
        iconContainer.BackgroundColor = Palette.Tint(tone);
        lblIcon.TextColor = color;
    }
}
