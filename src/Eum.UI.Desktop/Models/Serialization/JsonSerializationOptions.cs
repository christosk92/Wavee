using System.Text.Json.Serialization;

namespace Eum.UI.Models.Serialization;

public class JsonSerializationOptions
{
    private static readonly JsonSerializationOptions CurrentSettings = new();
	public static readonly JsonSerializationOptions Default = new();

	private JsonSerializationOptions()
	{
	}

	public JsonSerializationOptions Settings => CurrentSettings;
}
