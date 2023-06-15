using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModel.Library;

public class LibrariesViewModel
{
    public LibrariesViewModel()
    {
        Instance = this;
    }
    public static LibrariesViewModel Instance { get; private set; }

    public bool InLibrary(AudioId id)
    {
        return false;
    }

    public void SaveItem(Seq<AudioId> ids)
    {
    }
}