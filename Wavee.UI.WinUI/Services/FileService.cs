using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Wavee.UI.Interfaces.Services;

namespace Wavee.UI.WinUI.Services
{
    internal sealed class FileService : IFileService
    {
        private readonly IAppDataProvider _appDataProvider;

        public FileService(IAppDataProvider appDataProvider)
        {
            _appDataProvider = appDataProvider;
        }

        public async ValueTask<string> CopyToAppStorage(string path)
        {
            var appdata = _appDataProvider.GetAppDataRoot();
            var fileName = Path.GetFileName(path);
            var finalPath = Path.Combine(appdata, fileName);

            //create directory 
            Directory.CreateDirectory(appdata);

            await using var fs = File.OpenRead(path);
            await using var ws = File.Create(finalPath);
            await fs.CopyToAsync(ws);
            return finalPath;
        }

        public IEnumerable<FileInfo> EnumerateIn(string dirName)
        {
            var appdata = _appDataProvider.GetAppDataRoot();
            var path = Path.Combine(appdata, dirName);

            var usersDirInfo = new DirectoryInfo(path);
            var userDirExists = usersDirInfo.Exists;
            const string searchPattern = "*.json";
            const SearchOption searchOption = SearchOption.AllDirectories;
            if (!userDirExists)
            {
                return Enumerable.Empty<FileInfo>();
            }
            return usersDirInfo.EnumerateFiles(searchPattern, searchOption);
        }

        public async ValueTask Write<T>(T value, string relativePath)
        {
            var appdata = _appDataProvider.GetAppDataRoot();
            var path = Path.Combine(appdata, relativePath);
            
            //create directory 
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            await using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, value);
        }
    }
}
