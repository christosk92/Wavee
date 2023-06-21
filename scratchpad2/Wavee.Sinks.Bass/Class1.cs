using System.Runtime.InteropServices;
using ManagedBass;
using ManagedBass.Enc;

namespace Wavee.Sinks.Bass;

public class Class1
{
    int _req; // request number/counter
    int _chan; // stream handle

    public Class1()
    {
        var mainLib = Path.Combine(AppContext.BaseDirectory, "libs", "x64", "bass.dll");
        var encogg = Path.Combine(AppContext.BaseDirectory, "libs", "x64", "bassenc_ogg.dll");
        //move into base directory
        File.Copy(mainLib, Path.Combine(AppContext.BaseDirectory, "bass.dll"), true);

        ManagedBass.Bass.PluginLoad(encogg);

        ManagedBass.Bass.Init();
        // enable playlist processing
        ManagedBass.Bass.NetPlaylist = 1;

        using var stream = new FileStream("test.ogg", FileMode.Create);

        int r;
        r = ++_req; // increment the request counter for this request
        // var url = "http://stream.radioreklama.bg:80/radio1rock128";
        // var c = ManagedBass.Bass.CreateStream(stream, 0,
        //     BassFlags.StreamDownloadBlocks | BassFlags.StreamStatus | BassFlags.AutoFree, StatusProc,
        //     new IntPtr(r));
        
        var str = ManagedBass.Bass.CreateStream(48000, 2, BassFlags.Default, StreamProcedureType.Push);
        //dynamic stream
        
        
        ManagedBass.Bass.ChannelPlay(c);
        var chan = c;
    }


    void StatusProc(IntPtr buffer, int length, IntPtr user)
    {
        if (buffer != IntPtr.Zero
            && length == 0
            && user.ToInt32() == _req) // got HTTP/ICY tags, and this is still the current request
        {
            var status = Marshal.PtrToStringAnsi(buffer); // display status
        }
    }
    
}