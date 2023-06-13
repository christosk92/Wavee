using System.Text;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Wavee.UI.Core.Logging;
using Wavee.UI.Helpers;

namespace Wavee.UI.Core.Contracts;

public abstract class ConfigBase : ReactiveObject
{
	protected JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
	protected ConfigBase()
	{
	}

	protected ConfigBase(string filePath)
	{
		SetFilePath(filePath);
	}

	/// <inheritdoc />
	public string FilePath { get; private set; } = "";

	private object FileLocker { get; } = new();

	/// <inheritdoc />
	public void AssertFilePathSet()
	{
		if (string.IsNullOrWhiteSpace(FilePath))
		{
			throw new NotSupportedException($"{nameof(FilePath)} is not set. Use {nameof(SetFilePath)} to set it.");
		}
	}

	/// <inheritdoc />
	public bool CheckFileChange()
	{
		AssertFilePathSet();

		if (!File.Exists(FilePath))
		{
			throw new FileNotFoundException($"{GetType().Name} file did not exist at path: `{FilePath}`.");
		}

		string jsonString;
		lock (FileLocker)
		{
			jsonString = File.ReadAllText(FilePath, Encoding.UTF8);
		}

		var newConfigObject = Activator.CreateInstance(GetType())!;
		JsonConvert.PopulateObject(jsonString, newConfigObject, jsonSerializerSettings);

		return !AreDeepEqual(newConfigObject);
	}

	/// <inheritdoc />
	public virtual void LoadOrCreateDefaultFile()
	{
		AssertFilePathSet();
		JsonConvert.PopulateObject("{}", this);

		if (!File.Exists(FilePath))
		{
			Logger.LogInfo($"{GetType().Name} file did not exist. Created at path: `{FilePath}`.");
		}
		else
		{
			try
			{
				LoadFile();
			}
			catch (Exception ex)
			{
                Logger.LogInfo($"{GetType().Name} file has been deleted because it was corrupted. Recreated default version at path: `{FilePath}`.");
                Logger.LogWarning(ex);
			}
		}

		ToFile();
	}

	/// <inheritdoc />
	public virtual void LoadFile()
	{
		string jsonString;
		lock (FileLocker)
		{
			jsonString = File.ReadAllText(FilePath, Encoding.UTF8);
		}

		JsonConvert.PopulateObject(jsonString, this, jsonSerializerSettings);

		if (TryEnsureBackwardsCompatibility(jsonString))
		{
			ToFile();
		}
	}

	/// <inheritdoc />
	public void SetFilePath(string path)
	{
		FilePath = Guard.NotNullOrEmptyOrWhitespace(nameof(path), path, trim: true);
	}

	/// <inheritdoc />
	public bool AreDeepEqual(object otherConfig)
	{
		var serializer = JsonSerializer.Create(jsonSerializerSettings);
		var currentConfig = JObject.FromObject(this, serializer);
		var otherConfigJson = JObject.FromObject(otherConfig, serializer);
		return JToken.DeepEquals(otherConfigJson, currentConfig);
	}

	/// <inheritdoc />
	public void ToFile()
	{
		AssertFilePathSet();

		string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, jsonSerializerSettings);
		lock (FileLocker)
		{
			File.WriteAllText(FilePath, jsonString, Encoding.UTF8);
		}
	}

	public void DeleteMe()
	{
		var safeIO = new SafeIoManager(FilePath);
		safeIO.DeleteMe();
	}
	protected virtual bool TryEnsureBackwardsCompatibility(string jsonString) => true;
}
