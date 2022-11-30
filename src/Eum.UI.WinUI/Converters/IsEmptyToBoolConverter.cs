using System;
using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.Converters
{
    public class IsEmptyToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            var invert = false;
            if (parameter != null)
            {
                bool.TryParse(parameter.ToString(), out invert);
            }
            switch (value)
            {
                case null:
                    return invert;
                case string s:
                    return !string.IsNullOrWhiteSpace(s) && !invert;
                case IList list:
                    {
                        bool empty = list.Count == 0;
                        if (invert)
                            empty = !empty;
                        if (empty)
                            return false;
                        else
                            return true;
                    }
                case TimeSpan span when !invert:
                    return span != TimeSpan.Zero ? true : false;
                case TimeSpan span:
                    return span != TimeSpan.Zero;
                default:
                    {
                        if (decimal.TryParse(value.ToString(), out var number))
                        {
                            if (!invert)
                                return number > 0 ? true : false;
                            else
                                return number <= 0;

                        }

                        break;
                    }
            }

            return !invert;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}
