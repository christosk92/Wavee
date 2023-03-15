using Microsoft.Extensions.Logging;
using LiteDB;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using ImageMagick;
using Nito.AsyncEx;
using TagLib;
using File = System.IO.File;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Ocsp;
using Wavee.UI.Identity.Users.Contracts;

namespace Wavee.UI.AudioImport.Database
{
    internal class LiteDbAudioDb : IAudioDb
    {
        private readonly AsyncLock _lock = new();
        private readonly ILiteDatabase _db;
        private readonly string _workDir;
        private readonly string _audioLinks;
        private readonly ILogger<LiteDbAudioDb>? _logger;
        public ILiteCollection<LocalAudioFile> AudioFiles
        {
            get;
        }

        public LiteDbAudioDb(ILiteDatabase db, ILogger<LiteDbAudioDb>? logger, string workDir)
        {
            _db = db;
            _logger = logger;
            _workDir = workDir;
            _audioLinks = Path.Combine(workDir, "audio_links");
            AudioFiles = _db.GetCollection<LocalAudioFile>();
            AudioFiles.EnsureIndex(a => a.CreatedAt);
            AudioFiles.EnsureIndex(a => a.Artists.Select(k => k));
            AudioFiles.EnsureIndex(a => a.Album);
            AudioFiles.EnsureIndex(a => a.Path, true);

            Directory.CreateDirectory(_audioLinks);
            Directory.CreateDirectory(Path.Combine(_workDir, "images"));
        }

        public IReadOnlyCollection<LocalAudioFile>
            GetLatestAudioFiles<TK>(Expression<Func<LocalAudioFile, TK>> order,
                bool ascending, int offset = 0, int limit = 20)
        {
            return AudioFiles
                .Query()
                .OrderBy(order, ascending ? 1 : -1)
                .Skip(offset)
                .Limit(limit)
                .ToList();
        }

        // public string GetLinkedPathName(string path)
        // {
        //     var hash = GetHash(path);
        //     return Path.Combine(_audioLinks, hash);
        // }


        public IEnumerable<LocalAlbum> GetLatestAlbumsDb<TK>(Expression<Func<LocalAudioFile, TK>> order,
            bool ascending,
            int offset,
            int limit)
        {
            return AudioFiles
                .Query()
                .OrderBy(order, ascending ? 1 : -1)
                .ToEnumerable()
                .GroupBy(x => x.Album)
                .Select(x => new LocalAlbum
                {
                    Album = x.Key,
                    ServiceType = ServiceType.Local,
                    Image = x.FirstOrDefault(a => !string.IsNullOrEmpty(a.ImagePath))?.ImagePath,
                    Artists = x.SelectMany(a => a.Artists).ToArray(),
                    Tracks = x.Count(),
                    ReleaseYear = (ushort)x.First().Year
                })
                .Skip(offset)
                .Take(limit);
        }

        public IReadOnlyCollection<LocalAudioFile> GetTracksForAlbum(string albumName)
        {
            return AudioFiles
                .Find(a => a.Album == albumName)
                .OrderBy(a => a.Track)
                .ToList();
        }

        public IReadOnlyCollection<(string Path, DateTime LastUpdatedAt)> GetAll()
        {
            return AudioFiles
                .Find(_ => true)
                .Select(a => (a.Path, a.LastUpdatedAt))
                .ToList();
        }

        public bool Remove(string path)
        {
            return AudioFiles.Delete(path);
        }

        public void CleanupDuplicates()
        {
            //only keep item with latest LastUpdatedAt
            var duplicates = AudioFiles
                .FindAll()
                .GroupBy(a => a.Path)
                .Where(g => g.Count() > 1)
                .Select(g => g.OrderByDescending(a => a.LastUpdatedAt).Skip(1))
                .SelectMany(x => x)
                .ToList();
            foreach (var duplicate in duplicates)
            {
                AudioFiles.Delete(duplicate.Path);
            }
        }

        public async Task<LocalAudioFile> ImportAudioFile(ImportAudioRequest request)
        {
            //create a hardlink 
            // var fileLinkPath = GetLinkedPathName(request.Path);
            // if (!File.Exists(fileLinkPath))
            //     CreateHardLink(fileLinkPath, request.Path, IntPtr.Zero);

            using var file = TagLib.File.Create(request.Path);
            var images = file.Tag.Pictures;
            string? imagePath = null;
            if (images.Length != 0)
            {
                var image = images.First();
                var imageData = image.Data.Data;
                //check to see if we have the image saved already
                string base64 = Path.GetFileName(request.Path);
                if (!string.IsNullOrEmpty(request.Tag.Album))
                {
                    base64 = string.Join("_", request.Tag.Album.Split(Path.GetInvalidFileNameChars()));
                }
                imagePath = Path.Combine(_workDir, "images", base64);
                var exists = File.Exists(imagePath);
                if (!exists)
                {
                    await using var mem = ResizeImage(imageData, 300, 300);
                    await using var fs = File.Create(imagePath);
                    await fs.WriteAsync(imageData);
                    await fs.FlushAsync();
                }

            }


            var audioFile = new LocalAudioFile
            {
                // Image = imageGuid,
                ImagePath = imagePath,
                Path = request.Path,
                Title = file.Tag.Title ?? Path.GetFileNameWithoutExtension(request.Path),
                Album = file.Tag.Album ?? "Unknown Album",
                AlbumSort = file.Tag.AlbumSort,
                AlbumArtists = file.Tag.AlbumArtists,
                AlbumArtistsSort = file.Tag.AlbumArtistsSort,
                BeatsPerMinute = file.Tag.BeatsPerMinute,
                Comment = file.Tag.Comment,
                Composers = file.Tag.Composers,
                ComposersSort = file.Tag.ComposersSort,
                Conductor = file.Tag.Conductor,
                Disc = file.Tag.Disc,
                DiscCount = file.Tag.DiscCount,
                Genres = file.Tag.Genres,
                Grouping = file.Tag.Grouping,
                Lyrics = file.Tag.Lyrics,
                Performers = file.Tag.Performers.Length == 0 ? UNKNOWN_ARTISTS : file.Tag.Performers,
                PerformersSort = file.Tag.PerformersSort,
                TitleSort = file.Tag.TitleSort,
                Track = file.Tag.Track,
                TrackCount = file.Tag.TrackCount,
                Year = file.Tag.Year,
                DateTagged = file.Tag.DateTagged,
                Copyright = file.Tag.Copyright,
                Description = file.Tag.Description,
                Publisher = file.Tag.Publisher,
                Subtitle = file.Tag.Subtitle,
                PerformersRole = file.Tag.PerformersRole,
                LastUpdatedAt = File.GetLastWriteTime(request.Path),
                ISRC = file.Tag.ISRC,
                Duration = file.Properties.Duration.TotalMilliseconds,
                CreatedAt = DateTime.UtcNow,
            };
            if (AudioFiles.Exists(a => a.Path == audioFile.Path))
            {
                AudioFiles.Update(audioFile.Path, audioFile);
            }
            else
            {
                AudioFiles.Insert(audioFile);
            }

            return audioFile;
            //     }
            // }
        }

        public static string[] UNKNOWN_ARTISTS = new string[]
        {
            "Unknown Artist"
        };

        public LocalAudioFile? GetAudioFile(string path)
        {
            return AudioFiles.Find(a => a.Path == path).FirstOrDefault();
        }

        public int Count() => AudioFiles.Count();

        public bool[] CheckIfAudioFilesExist(IEnumerable<string> paths)
        {
            using (_lock.Lock())
            {
                return paths.Select(path => AudioFiles.Exists(a => a.Path == path)).ToArray();
            }
        }

        private static Stream ResizeImage(byte[] imageStream, int width, int height)
        {
            var settings = new MagickReadSettings
            {
                Width = width,
                Height = height
            };

            var memStream = new MemoryStream();
            using var image = new MagickImage(imageStream);
            var size = new MagickGeometry(width, height)
            {
                // This will resize the image to a fixed size without maintaining the aspect ratio.
                // Normally an image will be resized to fit inside the specified size.
                IgnoreAspectRatio = true
            };

            image.Format = MagickFormat.Png;
            image.Resize(size);

            image.Write(memStream);

            return memStream;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );


        private static string GetHash(string f)
        {
            using var fileStream = new FileStream(f, FileMode.Open, FileAccess.Read);
            using var mst = new MemoryStream();
            fileStream.CopyTo(mst);
            return ToMd5Hash(mst.ToArray());
        }

        public static string ToMd5Hash(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            using var md5 = MD5.Create();
            return string.Join("",
                md5.ComputeHash(bytes).Select(x => x.ToString("X2")));
        }
    }
    public record ImportAudioRequest(Tag Tag, string Path);
}