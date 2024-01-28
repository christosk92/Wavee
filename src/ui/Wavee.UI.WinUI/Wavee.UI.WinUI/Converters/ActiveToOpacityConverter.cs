using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.UI.Xaml;
using Wavee.UI.ViewModels.NowPlaying;

namespace Wavee.UI.WinUI.Converters;

public class ActiveToOpacityConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty ActiveIndexProperty = DependencyProperty.Register(nameof(ActiveIndex), typeof(int), typeof(ActiveToOpacityConverter), new PropertyMetadata(default(int)));
    private const double MaxOpacity = 1.0;
    private const double MinOpacity = 0.2; // Minimum opacity for the farthest lines


    public int ActiveIndex
    {
        get => (int)GetValue(ActiveIndexProperty);
        set => SetValue(ActiveIndexProperty, value);
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int idx = 0;
        if (ActiveIndex is -1) idx = 0;
        else idx = ActiveIndex;
        double OpacityDecrementPerStepDownward = 0.1;
        double OpacityDecrementPerStepUpwarwd = .15;

        double OpacityDownwardStart = 0.8;
        double OpacityUpwardStart = 0.25;
        var line = value as LyricsLineViewModel;
        if (line != null)
        {
            int distanceFromActive = line.Index - idx;

            double opacity;

            if (distanceFromActive > 0)
            {
                // Line is below the active line
                opacity = OpacityDownwardStart - ((distanceFromActive - 1) * OpacityDecrementPerStepDownward);
                opacity = Math.Max(MinOpacity, opacity);
            }
            else if (distanceFromActive < 0)
            {
                // Line is above the active line
                opacity = OpacityUpwardStart - (Math.Abs((distanceFromActive + 1)) * OpacityDecrementPerStepUpwarwd);
            }
            else
            {
                opacity = 1;
            }
            return opacity;
        }
        return MaxOpacity; // Default opacity

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
public class ActiveToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isActive = (bool)value;
        return isActive ? FontWeights.Bold : FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class ActiveToFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isActive = (bool)value;
        return isActive ? 24d : 18d; // Larger font for active, smaller for inactive
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}