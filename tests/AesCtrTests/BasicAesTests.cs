using AesCtr;

namespace AesCtrTests;

public class BasicAesTests
{
    [Fact]
    public void Test_Basic_Decryption()
    {
        const string key = "99-04-DE-B6-6B-B8-F5-63-63-4B-D0-03-D6-2F-BF-76";
        var keyBytes = key.Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
        var iv = new byte[]
            { 0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93 };

        var input =
            "1C FA 77 47 68 DC 16 47 5D E5 9F 3C 7F 57 59 6D EC F6 A7 F6 A2 3F AC 24 41 36 2D 49 76 58 A2 0E 6C 0A 91 92 C4 C7 40 B3 75 80 B7 14 1A A2 50 5A 5A 03 1C F9 9E E3 E3 B0 9D 44 2A 82 CA 9B D5 06 B1 49 FD D5 0A 11 6F 7F B6 37 6E F9 A2 CF AA A5 38 CF AB 54 7F F1 41 C8 56 21 03 FF 0C A9 92 1F 42 BF C3 F4 5B B7 26 E4 92 CB 8D 0E 0D CA 59 52 6B 32 40 F7 7E 48 72 95 3B 6E EB 6C CC 3F 43 AA 48 7D 09 04 C2 47 B3 41 CA 2B E3 70 38 DF 3F 01 95 C4 02 EA DF BB 10 7E 19 D1 74 74 35 B0 18 DE";
        var inputBytes = input.Split(' ').Select(x => Convert.ToByte(x, 16)).ToArray();
        var expectedOutput =
            "4F 67 67 53 00 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 45 8E BA 89 01 8B 81 6E 00 00 A0 B9 B4 00 11 79 A4 00 01 6C 47 58 54 3D 61 4F 55 63 81 90 7F 7D 97 9D 90 80 68 4A 2A 8F 62 63 A4 AD 95 A8 98 9B C1 AD 98 AC 8A 98 8B 94 9E 8C 90 96 54 93 46 67 66 46 3A 41 68 88 8B 63 80 AB 98 9B 80 69 38 53 71 BD 8B A3 AB A4 9E A6 B1 AE A4 9E B6 97 8C 9D 9E 96 96 91 93 86 6B 66 6A 6E 6F 74 42 8E 69 82 69 8D 76 8C 7E 3E 19 11 00 01 D8 A3 1C C1 98 F2 86 3F D8 A3 1C C1 98 F2 86 3F";
        var expectedOutputBytes = expectedOutput.Split(' ').Select(x => Convert.ToByte(x, 16)).ToArray();
        const int CryptChunkSize = 0x4000;
        var decrypted = new Aes128CtrTransform(keyBytes, iv, 0x4000);
        var output = new byte[inputBytes.Length];
        decrypted.TransformBlock(inputBytes, 0, inputBytes.Length, output, 0);
        Assert.Equal(expectedOutputBytes, output);
    }

    [Fact]
    public void Test_Seeking_Decryption()
    {
        const string key = "99-04-DE-B6-6B-B8-F5-63-63-4B-D0-03-D6-2F-BF-76";
        var keyBytes = key.Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
        var iv = new byte[]
            { 0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93 };

        using var input = File.OpenRead("short_input.bin");
        var expectedOutputBytes = File.ReadAllBytes("short_output.bin");

        using var decrypted = new Aes128CtrStream(input, keyBytes, iv);
        var output = new byte[expectedOutputBytes.Length];
        //read half of the file
        //we can only read in multiples of 16
        var readAmount = (int)input.Length / 2;
        //Value must be a multiple of 16
        readAmount -= readAmount % 16;
        //we can only read in chunks of 16
        for (var i = 0; i < readAmount; i += 16)
        {
            var k = decrypted.Read(output, i, 16);
        }

        //seek to random position
        var rnd = new Random();
        var seekPosition = rnd.Next(0, (int)input.Length / 2);
        //Value must be a multiple of 16
        seekPosition -= seekPosition % 16;
        decrypted.Seek(seekPosition, SeekOrigin.Begin); 
        //read the rest of the file
        for (var i = seekPosition; i < expectedOutputBytes.Length; i += 16)
        {
            var k = decrypted.Read(output, i, 16);
        }

        Assert.Equal(expectedOutputBytes, output);
    }
}