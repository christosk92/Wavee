# Wavee

# Note (6/12/2023)
Hey guys. I wanted to address some clarification on why there's still no release. While I've mentioned multiple times that the release is close, I have to admit that I'm not satisfied with the performance, particularly in the UI section. A while back I was not satisfied with the performance of the client library. It took me a while to iron that part out, and now I am really proud of the performance there. So now its the UI's turn.

Perhaps some of you find it adequate. For instance, my friend said, "What do you mean? It's fast enough." However, I do not agree, and I want to emphasize that I'm not being pedantic or something.

When I observe other applications loading 10,000 items in a list instantly, while mine takes 100 milliseconds (which is still a significant delay), I consider it to be poor performance. Our computers are insanely fast, and I really believe they should be capable of handling such trivial tasks efficiently.
Whether this is an issue with WinUI 3 or Windows App Sdk or my code, I do not know yet and I will continue to investigate.

My intention is to deliver the best and fastest experience possible, and I refuse to settle for subpar performance merely for the sake of releasing something.

# Disclaimer
I DO NOT encourage piracy and DO NOT support any form of downloader/recorder designed with the help of this repository and in general anything that goes against the Spotify ToS. 
I have built in a standard premium checker, and if any forks are made that override this requirement, I will pull the code.
Please please please, do not do this stuff, as it will endanger the future of this, and many other projects.

# Wavee 
Wavee is a alternative to the original Spotify desktop client for Windows.
It is written in native C#, in combination with WinUI (using Windows App Sdk), for a native Windows 11 Spotify experience. 
Playback is handles using NAudio.

![image](https://user-images.githubusercontent.com/13438702/211539400-25468ac1-2458-4b9e-b149-d27a5405a186.png)
