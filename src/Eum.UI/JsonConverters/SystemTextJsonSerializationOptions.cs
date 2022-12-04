using System.Text.Json;

namespace Eum.UI.JsonConverters
{
    public static class SystemTextJsonSerializationOptions
    {
        static SystemTextJsonSerializationOptions()
        {
            Default = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new ItemIdToJsonConverter()
                }
            };
        }

        public static JsonSerializerOptions Default { get; }
    }
}