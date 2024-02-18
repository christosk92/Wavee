﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Wavee.Spotify.Decryption;

internal sealed class BouncyCastleDecrypt 
{
    private readonly IBufferedCipher _cipher;
    private readonly KeyParameter _spec;
    private static BigInteger _ivInt = null!;
    private readonly int _chunkSize;
    private static readonly BigInteger IvDiff = BigInteger.ValueOf(0x100);
    
    public BouncyCastleDecrypt(byte[] key, byte[] iv, int chunkSize)
    {
        _chunkSize = chunkSize;
        _ivInt = new BigInteger(1, iv);
        _spec = ParameterUtilities.CreateKeyParameter("AES", key);
        _cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
    }

    
    public void Decrypt(int chunkIndex, byte[] chunk)
    {
        var iv = _ivInt!.Add(
            BigInteger.ValueOf(_chunkSize * chunkIndex / 16));
        for (var i = 0; i < chunk.Length; i += 4096)
        {
            _cipher.Init(true, new ParametersWithIV(_spec, iv.ToByteArray()));

            var c = Math.Min(4096, chunk.Length - i);
            var processed = _cipher.DoFinal(chunk,
                i,
                c,
                chunk, i);
            if (c != processed)
                throw new IOException(string.Format("Couldn't process all data, actual: {0}, expected: {1}",
                    processed, c));

            iv = iv.Add(IvDiff);
        }
    }
}