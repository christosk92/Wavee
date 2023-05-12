using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Wavee.UI.Helpers;

public static class EnvironmentHelpers
{
	// appName, dataDir
	private static ConcurrentDictionary<string, string> DataDirDict { get; } = new ConcurrentDictionary<string, string>();

	// Do not change the output of this function. Backwards compatibility depends on it.
	public static string GetDataDir(string appName)
	{
		if (DataDirDict.TryGetValue(appName, out string? dataDir))
		{
			return dataDir;
		}

		string directory;

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var home = Environment.GetEnvironmentVariable("HOME");
			if (!string.IsNullOrEmpty(home))
			{
				directory = Path.Combine(home, "." + appName.ToLowerInvariant());
				Debug.WriteLine($"Using HOME environment variable for initializing application data at `{directory}`.");
			}
			else
			{
				throw new DirectoryNotFoundException("Could not find suitable datadir.");
			}
		}
		else
		{
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");
			if (!string.IsNullOrEmpty(localAppData))
			{
				directory = Path.Combine(localAppData, appName);
                Debug.WriteLine($"Using APPDATA environment variable for initializing application data at `{directory}`.");
			}
			else
			{
				throw new DirectoryNotFoundException("Could not find suitable datadir.");
			}
		}

		if (Directory.Exists(directory))
		{
			DataDirDict.TryAdd(appName, directory);
			return directory;
		}

		Debug.WriteLine($"Creating data directory at `{directory}`.");
		Directory.CreateDirectory(directory);

		DataDirDict.TryAdd(appName, directory);
		return directory;
	}


	// This method removes the path and file extension.
	//
	// Given Wasabi releases are currently built using Windows, the generated assemblies contain
	// the hard coded "C:\Users\User\Desktop\WalletWasabi\.......\FileName.cs" string because that
	// is the real path of the file, it doesn't matter what OS was targeted.
	// In Windows and Linux that string is a valid path and that means Path.GetFileNameWithoutExtension
	// can extract the file name but in the case of OSX the same string is not a valid path so, it assumes
	// the whole string is the file name.
	public static string ExtractFileName(string callerFilePath)
	{
		var lastSeparatorIndex = callerFilePath.LastIndexOf("\\");
		if (lastSeparatorIndex == -1)
		{
			lastSeparatorIndex = callerFilePath.LastIndexOf("/");
		}

		var fileName = callerFilePath;

		if (lastSeparatorIndex != -1)
		{
			lastSeparatorIndex++;
			fileName = callerFilePath[lastSeparatorIndex..]; // From lastSeparatorIndex until the end of the string.
		}

		var fileNameWithoutExtension = fileName.TrimEnd(".cs", StringComparison.InvariantCultureIgnoreCase);
		return fileNameWithoutExtension;
	}

	public static bool IsFileTypeAssociated(string fileExtension)
	{
		// Source article: https://edi.wang/post/2019/3/4/read-and-write-windows-registry-in-net-core

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			throw new InvalidOperationException("Operation only supported on windows.");
		}

		fileExtension = fileExtension.TrimStart('.'); // Remove . if added by the caller.

		using var key = Registry.ClassesRoot.OpenSubKey($".{fileExtension}");

		// Read the (Default) value.
		return key?.GetValue(null) is not null;
	}

	public static string GetFullBaseDirectory()
	{
		var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			if (!fullBaseDirectory.StartsWith('/'))
			{
				fullBaseDirectory = fullBaseDirectory.Insert(0, "/");
			}
		}

		return fullBaseDirectory;
	}

	public static string GetExecutablePath()
	{
		var fullBaseDir = GetFullBaseDirectory();
		var wassabeeFileName = Path.Combine(fullBaseDir, Constants.ExecutableName);
		wassabeeFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{wassabeeFileName}.exe" : $"{wassabeeFileName}";
		if (File.Exists(wassabeeFileName))
		{
			return wassabeeFileName;
		}
		var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new NullReferenceException("Assembly or Assembly's Name was null.");
		var fluentExecutable = Path.Combine(fullBaseDir, assemblyName);
		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{fluentExecutable}.exe" : $"{fluentExecutable}";
	}
}
