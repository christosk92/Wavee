using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace Wavee.ViewModels.Bases;

public abstract class ConfigBase : INotifyPropertyChanged
{
	protected ConfigBase()
	{
	}

	protected ConfigBase(string filePath)
	{
		SetFilePath(filePath);
	}

	/// <remarks>
	/// Guards both storing to <see cref="FilePath"/> and retrieving contents of <see cref="FilePath"/>.
	/// <para>Otherwise, we risk concurrent read and write operations on <see cref="FilePath"/>.</para>
	/// </remarks>
	protected object FileLock { get; } = new();

	/// <inheritdoc/>
	public string FilePath { get; private set; } = "";

	/// <inheritdoc/>
	public void AssertFilePathSet()
	{
		if (string.IsNullOrWhiteSpace(FilePath))
		{
			throw new NotSupportedException($"{nameof(FilePath)} is not set. Use {nameof(SetFilePath)} to set it.");
		}
	}

	/// <inheritdoc />
	public virtual void LoadFile(bool createIfMissing = false)
	{
		if (createIfMissing)
		{
			AssertFilePathSet();

			lock (FileLock)
			{
				JsonConvert.PopulateObject("{}", this);

				if (!File.Exists(FilePath))
				{
					Log.Information($"{GetType().Name} file did not exist. Created at path: `{FilePath}`.");
				}
				else
				{
					try
					{
						LoadFileNoLock();
					}
					catch (Exception ex)
					{
						Log.Information($"{GetType().Name} file has been deleted because it was corrupted. Recreated default version at path: `{FilePath}`.");
						Log.Warning(ex, "Failed to load {ConfigType} file.", GetType().Name);
					}
				}

				ToFileNoLock();
			}
		}
		else
		{
			lock (FileLock)
			{
				LoadFileNoLock();
			}
		}
	}

	/// <inheritdoc />
	public void SetFilePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
		}
		
		FilePath = path;
		//FilePath = Guard.NotNullOrEmptyOrWhitespace(nameof(path), path, trim: true);
	}

	/// <inheritdoc />
	public void ToFile()
	{
		lock (FileLock)
		{
			ToFileNoLock();
		}
	}

	protected void LoadFileNoLock()
	{
		string jsonString = ReadFileNoLock();

		JsonConvert.PopulateObject(jsonString, this);
	}

	protected void ToFileNoLock()
	{
		AssertFilePathSet();

		string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
		WriteFileNoLock(jsonString);
	}

	protected void WriteFileNoLock(string contents)
	{
		File.WriteAllText(FilePath, contents, Encoding.UTF8);
	}

	protected string ReadFileNoLock()
	{
		return File.ReadAllText(FilePath, Encoding.UTF8);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
