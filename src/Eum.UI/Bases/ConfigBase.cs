using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Helpers;
using Eum.Logging;
using Eum.UI.Helpers;
using Eum.UI.JsonConverters;

namespace Eum.UI.Bases;

public abstract class ConfigBase 
{
    public string FilePath { get; set; } = "";

    private object FileLocker { get; } = new();


    /// <inheritdoc />
    public void ToFile()
    {
        if (string.IsNullOrEmpty(FilePath)) return;
        string jsonString = JsonSerializer.Serialize(This, SystemTextJsonSerializationOptions.Default);
        lock (FileLocker)
        {
            try
            {
                File.WriteAllText(FilePath, jsonString, Encoding.UTF8);
            }
            catch (IOException x)
            {

            }
        }
    }

    [JsonIgnore]
    public abstract object This { get; }
}
