using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Wavee.Metadata.Artist;

namespace Wavee.UI.WinUI;

public class HumanizeDateTimeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DiscographyReleaseDate dc)
        {
            return dc.Precision switch
            {
                ReleaseDatePrecisionType.Year => dc.Date.Year.ToString(),
                ReleaseDatePrecisionType.Month => $"{dc.Date:MMMM} {dc.Date.Year}",
                ReleaseDatePrecisionType.Day => $"{dc.Date.Day}, {dc.Date:MMMM} {dc.Date.Year}",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}