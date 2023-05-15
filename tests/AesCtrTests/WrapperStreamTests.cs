using AesCtr;

namespace AesCtrTests;

public class WrapperStreamTests
{
    [Fact]
    public void Test_Basic_Decryption()
    {
        const string key = "99-04-DE-B6-6B-B8-F5-63-63-4B-D0-03-D6-2F-BF-76";
        var keyBytes = key.Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
        var iv = new byte[]
            { 0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93 };

        using var input = File.OpenRead("short_input.bin");
        var expectedOutputBytes = File.ReadAllBytes("short_output.bin");

        using var decryptedOriginal = new Aes128CtrStream(input, keyBytes, iv);
        var decrypted = new Aes128CtrWrapperStream(decryptedOriginal);

        var output = new byte[input.Length];
        var read = decrypted.Read(output, 0, output.Length);
        Assert.Equal(expectedOutputBytes, output);
    }

    [Fact]
    public void Test_Seeking_To_Multiple_Of_16()
    {
        const string key = "99-04-DE-B6-6B-B8-F5-63-63-4B-D0-03-D6-2F-BF-76";
        var keyBytes = key.Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
        var iv = new byte[]
            { 0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93 };

        using var input = File.OpenRead("short_input.bin");
        var expectedOutputBytes = File.ReadAllBytes("short_output.bin");

        using var d = new Aes128CtrStream(input, keyBytes, iv);
        var decryptedOriginal = new Aes128CtrWrapperStream(d);

        var rnd = new Random();
        var seekPosition = rnd.Next(0, (int)input.Length / 2);
        //make sure we seek to a multiple of 16
        seekPosition = (seekPosition / 16) * 16;

        var seekedTo = decryptedOriginal.Seek(seekPosition, SeekOrigin.Begin);
        var output = new byte[input.Length - seekedTo];
        var read = decryptedOriginal.Read(output, 0, output.Length);

        var expectedOutput = new byte[read];
        Array.Copy(expectedOutputBytes, seekedTo, expectedOutput, 0, read);
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void Test_Seeking_To_NOT_Multiple_Of_16()
    {
        const string key = "99-04-DE-B6-6B-B8-F5-63-63-4B-D0-03-D6-2F-BF-76";
        var keyBytes = key.Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
        var iv = new byte[]
            { 0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93 };

        using var input = File.OpenRead("short_input.bin");
        var expectedOutputBytes = File.ReadAllBytes("short_output.bin");

        using var d = new Aes128CtrStream(input, keyBytes, iv);
        var decryptedOriginal = new Aes128CtrWrapperStream(d);

        var rnd = new Random();
        var seekPosition = rnd.Next(0, (int)input.Length / 2);
        //make sure we seek to a multiple of 16 and then add 1
        seekPosition = (seekPosition / 16) * 16 + 1;

        var seekedTo = decryptedOriginal.Seek(seekPosition, SeekOrigin.Begin);
        var output = new byte[input.Length - seekedTo];
        var read = decryptedOriginal.Read(output, 0, output.Length);

        var expectedOutput = new byte[read];
        Array.Copy(expectedOutputBytes, seekedTo, expectedOutput, 0, read);
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public void Test_Seeking_Then_SeekingBack_Decrypt()
    {
        const string key = "99-04-DE-B6-6B-B8-F5-63-63-4B-D0-03-D6-2F-BF-76";
        var keyBytes = key.Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
        var iv = new byte[]
            { 0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93 };

        using var input = File.OpenRead("short_input.bin");
        var expectedOutputBytes = File.ReadAllBytes("short_output.bin");

        using var d = new Aes128CtrStream(input, keyBytes, iv);
        var decryptedOriginal = new Aes128CtrWrapperStream(d);

        var rnd = new Random();
        var seekPosition = rnd.Next(0, (int)input.Length / 2);
        //make sure we seek to a multiple of 16
        seekPosition = (seekPosition / 16) * 16;
        
        var seekedTo = decryptedOriginal.Seek(seekPosition, SeekOrigin.Begin);
        
        var output = new byte[input.Length - seekedTo];
        
        var read = decryptedOriginal.Read(output, 0, output.Length);
        
        var expectedOutput = new byte[read];
        
        Array.Copy(expectedOutputBytes, seekedTo, expectedOutput, 0, read);
        
        Assert.Equal(expectedOutput, output);
        
        //seek to start
        decryptedOriginal.Seek(0, SeekOrigin.Begin);
        
        //decrypt again
        output = new byte[input.Length];
        read = decryptedOriginal.Read(output, 0, output.Length);
        
        Assert.Equal(expectedOutputBytes, output);
    }
}