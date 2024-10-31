namespace Wavee.Config
{
    /// <summary>
    /// Represents the configuration settings required for initializing and managing the Spotify client.
    /// </summary>
    public sealed class SpotifyConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyConfig"/> class with specified playback configuration and credentials cache.
        /// </summary>
        /// <param name="playback">The Spotify playback configuration settings.</param>
        /// <param name="credentialsCache">The Spotify credentials cache for managing authentication tokens.</param>
        public SpotifyConfig(SpotifyPlaybackConfig playback, SpotifyCredentialsCache credentialsCache)
        {
            Playback = playback ?? throw new ArgumentNullException(nameof(playback));
            CredentialsCache = credentialsCache ?? throw new ArgumentNullException(nameof(credentialsCache));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyConfig"/> class with specified playback configuration.
        /// A new instance of <see cref="SpotifyCredentialsCache"/> is created by default.
        /// </summary>
        /// <param name="playback">The Spotify playback configuration settings.</param>
        public SpotifyConfig(SpotifyPlaybackConfig playback)
        {
            Playback = playback ?? throw new ArgumentNullException(nameof(playback));
            CredentialsCache = new SpotifyCredentialsCache();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyConfig"/> class with specified credentials cache.
        /// A new instance of <see cref="SpotifyPlaybackConfig"/> is created by default.
        /// </summary>
        /// <param name="credentialsCache">The Spotify credentials cache for managing authentication tokens.</param>
        public SpotifyConfig(SpotifyCredentialsCache credentialsCache)
        {
            Playback = new SpotifyPlaybackConfig();
            CredentialsCache = credentialsCache ?? throw new ArgumentNullException(nameof(credentialsCache));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyConfig"/> class with default playback configuration and credentials cache.
        /// </summary>
        public SpotifyConfig()
        {
            Playback = new SpotifyPlaybackConfig();
            CredentialsCache = new SpotifyCredentialsCache();
        }

        /// <summary>
        /// Gets the Spotify playback configuration settings.
        /// </summary>
        public SpotifyPlaybackConfig Playback { get; } = new SpotifyPlaybackConfig();

        /// <summary>
        /// Gets the Spotify credentials cache for managing authentication tokens.
        /// </summary>
        public SpotifyCredentialsCache CredentialsCache { get; } = new SpotifyCredentialsCache();

        /// <summary>
        /// Gets the optional caching configuration settings.
        /// If not provided, caching is disabled.
        /// </summary>
        public SpotifyCacheConfig? Cache { get; init; }

        private string _language = "en";

        /// <summary>
        /// Gets or sets the preferred language for Spotify requests.
        /// Must be a two-letter ISO 639-1 language code (e.g., "en", "es", "fr").
        /// Defaults to "en" if not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the language code is not two letters.</exception>
        public string Language
        {
            get => _language;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.Length != 2)
                {
                    throw new ArgumentException("Language must be a two-letter ISO 639-1 code.", nameof(Language));
                }
                _language = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="SpotifyConfig"/> with caching enabled.
        /// </summary>
        /// <param name="playback">The Spotify playback configuration settings.</param>
        /// <param name="credentialsCache">The Spotify credentials cache for managing authentication tokens.</param>
        /// <param name="cacheConfig">The caching configuration settings.</param>
        /// <returns>A configured instance of <see cref="SpotifyConfig"/> with caching.</returns>
        public static SpotifyConfig WithCaching(SpotifyPlaybackConfig playback, SpotifyCredentialsCache credentialsCache, SpotifyCacheConfig cacheConfig)
        {
            var config = new SpotifyConfig(playback, credentialsCache)
            {
                Cache = cacheConfig
            };
            return config;
        }
    }
}
