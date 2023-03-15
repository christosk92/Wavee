namespace Wavee.UI.AudioImport;
public interface IPlaycountService
{
    ulong GetPlaycount(string id);
    ulong[] GetPlaycounts(string[] ids);
}
