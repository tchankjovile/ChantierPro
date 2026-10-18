using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChantierPro.Mobile.Models;

/// <summary>Pastille de filtre sélectionnable (statuts, périodes, catégories…).</summary>
public partial class ChipItem : ObservableObject
{
    public ChipItem(string text, object? value, Action<ChipItem> onSelected, bool isSelected = false)
    {
        Text = text;
        Value = value;
        _isSelected = isSelected;
        SelectCommand = new RelayCommand(() => onSelected(this));
    }

    public string Text { get; }
    public object? Value { get; }
    public ICommand SelectCommand { get; }

    [ObservableProperty]
    private bool _isSelected;
}
