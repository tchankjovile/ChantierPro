using System.Windows.Input;

namespace ChantierPro.Mobile.Controls;

public partial class EmptyStateView : VerticalStackLayout
{
    public static readonly BindableProperty IconGlyphProperty =
        BindableProperty.Create(nameof(IconGlyph), typeof(string), typeof(EmptyStateView), string.Empty,
            propertyChanged: (b, o, n) => ((EmptyStateView)b).lblIcon.Text = (string)n);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyStateView), "Aucun élément",
            propertyChanged: (b, o, n) => ((EmptyStateView)b).lblTitle.Text = (string)n);

    public static readonly BindableProperty DescriptionProperty =
        BindableProperty.Create(nameof(Description), typeof(string), typeof(EmptyStateView), string.Empty,
            propertyChanged: (b, o, n) => ((EmptyStateView)b).lblDescription.Text = (string)n);

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyStateView), string.Empty,
            propertyChanged: (b, o, n) =>
            {
                var view = (EmptyStateView)b;
                view.btnAction.Text = (string)n;
                view.btnAction.IsVisible = !string.IsNullOrWhiteSpace((string)n);
            });

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateView), null,
            propertyChanged: (b, o, n) => ((EmptyStateView)b).btnAction.Command = (ICommand)n);

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand ActionCommand
    {
        get => (ICommand)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public EmptyStateView()
    {
        InitializeComponent();
    }
}
