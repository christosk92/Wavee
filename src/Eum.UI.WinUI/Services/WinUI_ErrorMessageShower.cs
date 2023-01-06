using Eum.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.Services
{
    internal class WinUI_ErrorMessageShower: IErrorMessageShower
    {
        public async Task ShowErrorAsync(Exception notImplementedException, string title, string description)
        {
            var content = new StackPanel();

            content.Children.Add(new TextBlock
            {
                Text = description
            });
            content.Children.Add(new TextBlock
            {
                Margin = new Thickness(0,8,0,4),
                Text = "Description",
                FontWeight = FontWeights.SemiBold,
                Opacity = .7
            });
            content.Children.Add(new TextBlock
            {
                Text = notImplementedException.ToString(),
            });
            var cd = new ContentDialog
            {
                XamlRoot = App.MWindow.Content.XamlRoot,
                Title = title,
                Content = content,
                PrimaryButtonText = "Ok", 
                SecondaryButtonText = "Report"
            };
            await cd.ShowAsync();
        }
    }
}
