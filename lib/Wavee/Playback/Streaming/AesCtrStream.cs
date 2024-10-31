using System.Numerics;
using System.Security.Cryptography;

namespace Wavee.Playback.Streaming;

public class AesCtrStream : Stream
{
    private readonly Stream _baseStream; // Encrypted data stream
    private readonly Aes _aes;
    private readonly ICryptoTransform _encryptor;
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly int _blockSize; // AES block size in bytes (16)
    private long _position;
    public Stream BaseStream => _baseStream;


    public AesCtrStream(Stream baseStream, byte[] key, byte[] iv)
    {
        _baseStream = baseStream;
        _key = key;
        _iv = iv;
        _blockSize = 16;
        _position = 0;

        _aes = Aes.Create();
        _aes.Key = key;
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.None;
        _encryptor = _aes.CreateEncryptor();
    }


    public override bool CanRead => _baseStream.CanRead;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => false; // Read-only stream

    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos = offset;
        switch (origin)
        {
            case SeekOrigin.Begin:
                newPos = offset;
                break;
            case SeekOrigin.Current:
                newPos = _position + offset;
                break;
            case SeekOrigin.End:
                newPos = Length + offset;
                break;
        }

        if (newPos < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Attempt to seek before start of stream");
        _position = newPos;
        return _position;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= Length)
            return 0;

        // Adjust count if beyond end of stream
        if (_position + count > Length)
            count = (int)(Length - _position);

        
        int totalBytesRead = 0;
        int bufferOffset = offset;

        while (totalBytesRead < count)
        {
            // Compute block index and offset within block, including initialOffset
            long blockIndex = _position / _blockSize;
            int blockOffset = (int)(_position % _blockSize);

            // Number of bytes we can process in current block
            int bytesInCurrentBlock = _blockSize - blockOffset;

            // Number of bytes left to read
            int bytesRemaining = count - totalBytesRead;

            // Number of bytes to read in this iteration
            int bytesToRead = Math.Min(bytesInCurrentBlock, bytesRemaining);

            // Read encrypted data
            byte[] encryptedBlock = new byte[bytesToRead];
            _baseStream.Seek(_position, SeekOrigin.Begin);
            int bytesRead = _baseStream.Read(encryptedBlock, 0, bytesToRead);
            if (bytesRead == 0)
                break; // End of base stream

            // Generate keystream block
            byte[] counterBlock = GetCounterBlock(blockIndex);
            byte[] keystreamBlock = new byte[_blockSize];
            _encryptor.TransformBlock(counterBlock, 0, _blockSize, keystreamBlock, 0);

            // Decrypt the block
            for (int i = 0; i < bytesRead; i++)
            {
                buffer[bufferOffset + i] = (byte)(encryptedBlock[i] ^ keystreamBlock[blockOffset + i]);
            }

            // Update positions
            bufferOffset += bytesRead;
            _position += bytesRead;
            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }



    private byte[] GetCounterBlock(long blockIndex)
    {
        byte[] counterBlock = new byte[_blockSize];
        Buffer.BlockCopy(_iv, 0, counterBlock, 0, _blockSize);

        // Convert blockIndex to a 16-byte big-endian array
        byte[] blockIndexBytes = new byte[_blockSize];
        BigInteger blockIndexBigInt = new BigInteger(blockIndex);
        byte[] blockIndexBigEndian = blockIndexBigInt.ToByteArray(isUnsigned: true, isBigEndian: true);

        // Copy blockIndexBigEndian into blockIndexBytes (aligned to the right)
        int offset = _blockSize - blockIndexBigEndian.Length;
        Buffer.BlockCopy(blockIndexBigEndian, 0, blockIndexBytes, offset, blockIndexBigEndian.Length);

        // Add blockIndexBytes to counterBlock
        int carry = 0;
        for (int i = _blockSize - 1; i >= 0; i--)
        {
            int sum = counterBlock[i] + blockIndexBytes[i] + carry;
            counterBlock[i] = (byte)(sum & 0xFF);
            carry = sum >> 8;
        }

        return counterBlock;
    }


    public override void SetLength(long value)
    {
        throw new NotSupportedException("SetLength not supported");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Write not supported");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _encryptor?.Dispose();
            _aes?.Dispose();
            _baseStream?.Dispose();
        }

        base.Dispose(disposing);
    }
}