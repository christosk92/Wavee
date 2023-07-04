using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace Wavee.UI.ViewModel.Shell.Lyrics;

public class LyricsLineViewModel : ObservableObject
{
    private bool _isActive;

    //[ObservableProperty] private bool _isActive;
    public string Words { get; init; }
    public double StartsAt { get; init; }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public double ToFontSize(bool b, double s, string s1)
    {
        //always parse (dot) as decimal separator
        var d = double.Parse(s1, CultureInfo.InvariantCulture);
        return b ? s : d;
    }
}