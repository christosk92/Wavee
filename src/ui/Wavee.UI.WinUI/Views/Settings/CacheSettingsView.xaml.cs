using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CacheSettingsView : Page
    {
        public CacheSettingsView()
        {
            this.InitializeComponent();
        }
        public SettingsViewModel<WaveeUIRuntime> ViewModel => SettingsViewModel<WaveeUIRuntime>.Instance;

        private void LanguageBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //display cache
            var selected = (AppLocale)LanguageBox.SelectedItem;
            ViewModel.MetadataCachePath = Path.Combine(ViewModel.MetadataCachePathBase,
                $"cache_{selected.Culture.TwoLetterISOLanguageName}.db");
        }


        public string CalculateSizeAndHumanize(string path)
        {
            return Humanize(CalculateSize(path));
        }
        private static string Humanize(long bytes)
        {
            //return "0.00 MB";
            //if bytes < 1kb, return bytes
            //if bytes < 1mb, return kb
            //if bytes < 1gb, return mb
            //if bytes < 1tb, return gb

            const long ONE_KB = 1024;
            if (bytes < ONE_KB)
                return $"{bytes} bytes";

            const long ONE_MB = 1024 * 1024;
            if (bytes < ONE_MB)
                return $"{bytes / 1024.0:0.00} KB";

            const long ONE_GB = 1024 * 1024 * 1024;
            if (bytes < ONE_GB)
                return $"{bytes / (1024.0 * 1024.0):0.00} MB";

            const long ONE_TB = (long)1024 * 1024 * 1024 * 1024;
            if (bytes < ONE_TB)
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.00} GB";

            return $"{bytes / (1024.0 * 1024.0 * 1024.0 * 1024.0):0.00} TB";
        }

        private static long CalculateSize(string path)
        {
            //if directory, calculate size of all files in directory
            //if file, calculate size of file
            if (string.IsNullOrWhiteSpace(path))
                return 0;

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                return files.Sum(file => new FileInfo(file).Length);
            }

            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            return 0;
        }
    }
}
