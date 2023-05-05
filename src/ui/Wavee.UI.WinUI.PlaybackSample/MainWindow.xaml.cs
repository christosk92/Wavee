using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Wavee.UI.WinUI.PlaybackSample
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            Instance = this;
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;
        }

        public void Refresh()
        {
            if (this.Content is MainContent mainContent)
            {
                this.Content = new MainContent(mainContent.ViewModel);
            }
            else
            {
                this.Content = new MainContent();
            }
        }
        public static MainWindow Instance { get; private set; }
    }
}
