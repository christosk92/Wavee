using System.Text;
using Eum.Logging;
using Eum.UI.Helpers;
using Eum.UI.Models.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Eum.UI.Bases;

public abstract class ConfigBase : NotifyPropertyChangedBase
{
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
		JsonConvert.PopulateObject(jsonString, newConfigObject, NewtonsoftJsonSerializationOptions.Default.Settings);

		return !AreDeepEqual(newConfigObject);
	}

	/// <inheritdoc />
	public virtual void LoadOrCreateDefaultFile()
	{
		AssertFilePathSet();
		JsonConvert.PopulateObject("{}", this);

		if (!File.Exists(FilePath))
		{
			S_Log.Instance.LogInfo($"{GetType().Name} file did not exist. Created at path: `{FilePath}`.");
		}
		else
		{
			try
			{
				LoadFile();
			}
			catch (Exception ex)
			{
                S_Log.Instance.LogInfo($"{GetType().Name} file has been deleted because it was corrupted. Recreated default version at path: `{FilePath}`.");
                S_Log.Instance.LogWarning(ex);
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

		JsonConvert.PopulateObject(jsonString, this, NewtonsoftJsonSerializationOptions.Default.Settings);

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
		var serializer = JsonSerializer.Create(NewtonsoftJsonSerializationOptions.Default.Settings);
		var currentConfig = JObject.FromObject(this, serializer);
		var otherConfigJson = JObject.FromObject(otherConfig, serializer);
		return JToken.DeepEquals(otherConfigJson, currentConfig);
	}

	/// <inheritdoc />
	public void ToFile()
	{
		AssertFilePathSet();

		string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, NewtonsoftJsonSerializationOptions.Default.Settings);
		lock (FileLocker)
		{
			File.WriteAllText(FilePath, jsonString, Encoding.UTF8);
		}
	}

	protected virtual bool TryEnsureBackwardsCompatibility(string jsonString) => true;
}
