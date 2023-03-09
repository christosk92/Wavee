using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Wavee.UI.Navigation;

namespace Wavee.UI.WinUI.XamlConverters
{
    internal sealed class ViewLocator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            //Vm --> view 
            //rename ViewModel to View
            if (value is INavigatable navigatable)
            {
                var itemType = navigatable.GetType();
                var name = itemType.FullName!.Replace("ViewModel", "View")
                    .Replace("Wavee.UI", "Wavee.UI.WinUI");
                var type = Type.GetType(name);

                if (type == null)
                {
                    //check base type
                    name = itemType.BaseType.FullName!.Replace("ViewModel", "View")
                        .Replace("Eum.UI", "Eum.UI.WinUI");
                    type = Type.GetType(name);
                }
                if (type != null && type != typeof(object))
                {

                    return (Control)Activator.CreateInstance(type, value)!;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
