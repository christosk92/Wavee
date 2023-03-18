namespace Wavee.UI.Interfaces.Services
{
    public interface IFileService
    {
        ValueTask<string> CopyToAppStorage(string path);
        IEnumerable<FileInfo> EnumerateIn(string dirName);

        ValueTask Write<T>(T value, string relativePath);
    }
}
