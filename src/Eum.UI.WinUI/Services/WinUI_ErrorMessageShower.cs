using Eum.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Eum.UI.WinUI.Services
{
    internal class WinUI_ErrorMessageShower: IErrorMessageShower
    {
        public async Task ShowErrorAsync(Exception notImplementedException, string title, string description)
        {
            var content = new StackPanel
            {
                Width = 600
            };
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

            // Background="{ThemeResource LayerFillColorDefaultBrush}"
            //BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
            var bd = new Border
            {
                BorderBrush = (SolidColorBrush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                Background = (SolidColorBrush) Application.Current.Resources["LayerFillColorDefaultBrush"],
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new (1)
            };
            var rtb = new RichTextBlock
            {
                TextWrapping = TextWrapping.Wrap,
            };
            var pg = new Paragraph();
            pg.Inlines.Add(new Run
            {
                Text = notImplementedException.ToString(),
            });
            rtb.Blocks.Add(pg);

            bd.Child = rtb;
            content.Children.Add(bd);
            var cd = new ContentDialog
            {
                XamlRoot = App.MWindow.Content.XamlRoot,
                Title = title,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = content,
                PrimaryButtonText = "Ok", 
                SecondaryButtonText = "Report",
                PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"]
            };
            await cd.ShowAsync();
        }
    }
}
