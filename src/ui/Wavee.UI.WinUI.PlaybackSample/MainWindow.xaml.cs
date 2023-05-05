using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.PlaybackSample
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            Instance = this;
            this.InitializeComponent();
        }

        public void Refresh()
        {
            if (this.Content is MainContent mainContent)
            {
                mainContent.Cleanup();
            }

            this.Content = new MainContent();
        }
        public static MainWindow Instance { get; private set; }
    }
}
