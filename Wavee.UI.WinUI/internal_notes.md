Basically
A music player that can both connect to Spotify and play local files (seperate services, so either local or spotify)

I want to reshare a lot of code, but obviously implementations are gonna be different.
I want a couple of pages :

-  Home page : This is different per service. For spotify this will be the users most listened tracks this month. For offline playback this will be the latest imported tracks.
- A library page: For spotify this will be a page with the users saved albums, songs, artists. For offline this will be a page with all the imported tracks, grouped by album, artists, or plain songs etc.
- a playlist page: TBD 


# Connection

For offline we create a local profile on disk, and store the users music there. For spotify we use the spotify api to get the users data.

So for that we need to have a login page that can connect to spotify, and a login page that can connect to the local profile.
Maybe we can use the same login page, and just have a toggle for local/spotify.

# Home page