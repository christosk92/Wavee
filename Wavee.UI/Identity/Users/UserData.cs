using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Utils;

namespace Wavee.UI.Identity.Users
{
    [JsonObject(MemberSerialization.OptIn)]
    public record UserData
    {
        public UserData(string workDir,
            ServiceType serviceType,
            string Username,
            string? DisplayName,
            string? ProfilePicture,
            Dictionary<string, string> Metadata)
        {
            var filePath = Path.Combine(workDir, serviceType.ToString(), $"{Username}.json");

            SetFilePath(filePath);

            this.Username = Username;
            this.DisplayName = DisplayName;
            this.ProfilePicture = ProfilePicture;
            this.Metadata = Metadata;

            ToFile();
        }
        [JsonProperty(PropertyName = "Username")]
        public string Username { get; init; }
        [JsonProperty(PropertyName = "DisplayName")]
        public string? DisplayName { get; init; }
        [JsonProperty(PropertyName = "ProfilePicture")]
        public string? ProfilePicture { get; init; }
        [JsonProperty(PropertyName = "Metadata")]
        public IReadOnlyDictionary<string, string> Metadata { get; init; }

        public static UserData FromFile(string filePath)
        {
            filePath = Guard.NotNullOrEmptyOrWhitespace(nameof(filePath), filePath);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"User file not found at: `{filePath}`.");
            }


            SafeIoManager safeIoManager = new(filePath);
            var jsonString = safeIoManager.ReadAllText(Encoding.UTF8);

            var km = JsonConvert.DeserializeObject<UserData>(jsonString, NewtonsoftJsonOptions.Options)
                            ?? throw new JsonSerializationException($"User file at: `{filePath}` is not a valid user file or it is corrupted.");

            km.SetFilePath(filePath);

            return km;
        }
        // `CriticalStateLock` is aimed to synchronize read/write access to the "critical" properties
        private object CriticalStateLock { get; } = new();
        public void ToFile()
        {
            if (FilePath is { } filePath)
            {
                ToFile(filePath);
            }
        }
        public void ToFile(string filePath)
        {
            string jsonString = string.Empty;

            lock (CriticalStateLock)
            {
                jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, NewtonsoftJsonOptions.Options);
            }

            IoHelpers.EnsureContainingDirectoryExists(filePath);

            SafeIoManager safeIoManager = new(filePath);
            safeIoManager.WriteAllText(jsonString, Encoding.UTF8);
        }
        public string? FilePath { get; private set; }
        public void SetFilePath(string? filePath)
        {
            FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath;
            if (FilePath is null)
            {
                return;
            }

            IoHelpers.EnsureContainingDirectoryExists(FilePath);
        }
    }
}
