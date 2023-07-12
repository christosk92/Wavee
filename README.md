# Wavee

# Note 7/12/2023

There has been some circumstances that are personal in my life. I will be taking a break from the project. 


![image](https://github.com/christosk92/Wavee/assets/13438702/e7431549-f6f7-4093-a421-38e247356a20)





# Note 6/19/2023
Spotify recently killed a lot of the internal APIs I relied on. 
Specifically the main connection over TCP will be phased out soon I assume, so I will convert everything to HTTPs, inlcuding authentication which will work using oauth.
This change is gonna require some time..



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

![image](https://github.com/christosk92/Wavee/assets/13438702/c9795767-2d7d-497f-97b9-4b562821a9db)


![image](https://github.com/christosk92/Wavee/assets/13438702/5ae4655d-d3e1-47b6-b72e-406b77f4c3f1)

