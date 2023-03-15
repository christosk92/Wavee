using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Utils;

namespace Wavee.UI.Identity.Users
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserData : ObservableObject
    {
        private double _sidebarWidth = 200;
        private bool _sidebarExpanded;

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
            if (!File.Exists(filePath))
            {
                ToFile();
            }
            else
            {
                LoadFile();
            }
            this.PropertyChanged += (sender, args) =>
            {
                if (_allowedPropertyNames.Contains(args.PropertyName))
                {
                    ToFile();
                }
            };
        }
        [JsonProperty(PropertyName = "Username")]
        public string Username
        {
            get; init;
        }
        [JsonProperty(PropertyName = "DisplayName")]
        public string? DisplayName
        {
            get; init;
        }
        [JsonProperty(PropertyName = "ProfilePicture")]
        public string? ProfilePicture
        {
            get; init;
        }
        [JsonProperty(PropertyName = "Metadata")]
        public IReadOnlyDictionary<string, string> Metadata
        {
            get; init;
        }

        [JsonProperty(PropertyName = "SidebarWidth")]
        public double SidebarWidth
        {
            get => _sidebarWidth;
            set => SetProperty(ref _sidebarWidth, value);
        }

        [JsonProperty(PropertyName = "IsSidebarImageExpanded")]
        public bool SidebarExpanded
        {
            get => _sidebarExpanded;
            set => SetProperty(ref _sidebarExpanded, value);
        }
        private static string[] _allowedPropertyNames = new string[]
        {
            nameof(SidebarWidth),
            nameof(SidebarExpanded)
        };
        // protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        // {
        //     if (_allowedPropertyNames.Contains(e.PropertyName))
        //     {
        //         ToFile();
        //     }
        // }

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

        public void ToFile()
        {
            lock (FileLock)
            {
                ToFileNoLock();
            }
        }
        public void LoadFile(bool createIfMissing = false)
        {
            if (createIfMissing)
            {
                AssertFilePathSet();

                lock (FileLock)
                {
                    JsonConvert.PopulateObject("{}", this);

                    if (!File.Exists(FilePath))
                    {
                        Debug.WriteLine($"{GetType().Name} file did not exist. Created at path: `{FilePath}`.");
                    }
                    else
                    {
                        try
                        {
                            LoadFileNoLock();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"{GetType().Name} file has been deleted because it was corrupted. Recreated default version at path: `{FilePath}`.");
                            Debug.WriteLine(ex);
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
        protected void ToFileNoLock()
        {
            AssertFilePathSet();

            string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, NewtonsoftJsonOptions.Options);
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
        protected void LoadFileNoLock()
        {
            string jsonString = ReadFileNoLock();

            JsonConvert.PopulateObject(jsonString, this, NewtonsoftJsonOptions.Options);
        }
        public string? FilePath
        {
            get; private set;
        }
        public void SetFilePath(string? filePath)
        {
            FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath;
            if (FilePath is null)
            {
                return;
            }

            IoHelpers.EnsureContainingDirectoryExists(FilePath);
        }

        /// <inheritdoc/>
        public void AssertFilePathSet()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                throw new NotSupportedException($"{nameof(FilePath)} is not set. Use {nameof(SetFilePath)} to set it.");
            }
        }
        /// <remarks>
        /// Guards both storing to <see cref="FilePath"/> and retrieving contents of <see cref="FilePath"/>.
        /// <para>Otherwise, we risk concurrent read and write operations on <see cref="FilePath"/>.</para>
        /// </remarks>
        protected object FileLock { get; } = new();
    }
}
