# Wavee

## Note

I was trying to set-up the project in way that should work for NOT just Spotify, but also Apple Music, Soundcloud etc. 
However after much deliberation I have decided it is not worth the effort, and better solutions exist out there. 
So I eventually gave-up, and decided to just focus on Spotify.

## General
Wavee.Spotify is a high performant (meaning low cpu/network/memory usage) Spotify client, written in native C#, without any interop. 
Playback is powered by NAudio.
Everything that you would expect the normal Spotify client to have, *should* be included. Obviously some things may be missing as Spotify changes their stuff. But the core functionality (remote control, playback, metadata fetching) should work fine.

Right now, the core is built to talk with Spotify over a raw TCP connection, instead of traditional rest calls. It is unsure for how long this will continue to function, but I have found that requests over this protocol, are much much faster than traditional https.
You can check the full implementation under [/src/lib/Wavee.Spotify/Infrastructure/Connection/SpotifyConnection.cs](/src/lib/Wavee.Spotify/Infrastructure/Connection/SpotifyConnection.cs)

I regularly try out different stuff in a scratchpad, so if you seea commit titled: "promoted from scratchpad", it probably means that there was a large architectural change.

## Updates
I will try to post regular updates about what I've done, and what needs to be done. Don't expect too much though :)

### 2023/05/19 
- Refactored connection logic: Removed async (which made it much faster), and removed unnecesary heap allocations by replacing them with ``stackalloc``.
- Refactor player handler: Make sure access to player states are thread-safe and do not cause side effects.
- Added basic caching of encrypted audio files (on disk), and track metadata in a sqlite database. This is optional and can be turned on/off using the Config like:
   ```cs
   _config = new SpotifyConfig(
            CachePath: cachePath,
            Remote: new SpotifyRemoteConfig(
                DeviceName: "Wavee",
                DeviceType: DeviceType.Computer
            ),
            Playback: new SpotifyPlaybackConfig(
                PreferredQualityType.Normal,
                CrossfadeDuration: Option<TimeSpan>.None,
                Autoplay: true
            )
        );
        ```


![image](https://user-images.githubusercontent.com/13438702/211539400-25468ac1-2458-4b9e-b149-d27a5405a186.png)
