using Newtonsoft.Json;

namespace Wavee.UI.Utils
{
    public static class NewtonsoftJsonOptions
    {
        static NewtonsoftJsonOptions()
        {
            Options = new JsonSerializerSettings();
        }
        public static JsonSerializerSettings Options { get; }
    }
}
