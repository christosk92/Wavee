using Wavee.ViewModels.Interfaces;

namespace ConsoleApp1.PseudoUI;

public class MainWindowFactory : IMainWindowFactory
{
    public IMainWindow Create()
    {
        return new MainWindow();
    }
}