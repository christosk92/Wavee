// using Wavee.Interfaces;
// using Wavee.Playback.Player;
// using Wavee.Playback.Streaming;
//
// namespace ConsoleApp1;
//
// public sealed class LocalFileAudioStream : AudioStream
// {
//     private readonly FileStream _audioStreamImplementation;
//     private WaveeAudioStream.WaveeSampleProvider _sampleProvider;
//
//     public LocalFileAudioStream(WaveePlayerMediaItem mediaItem, string filePath) : base(mediaItem)  
//     {
//         _audioStreamImplementation = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
//     }
//
//
//     public override Task InitializeAsync(CancellationToken cancellationToken)
//     {
//         using var taglibFile = TagLib.File.Create(_audioStreamImplementation.Name);
//         TotalDuration = taglibFile.Properties.Duration;
//         return Task.CompletedTask;
//     }
//
//     public override bool CanRead => _audioStreamImplementation.CanRead;
//
//     public override bool CanSeek => _audioStreamImplementation.CanSeek;
//
//     public override bool CanWrite => _audioStreamImplementation.CanWrite;
//
//     public override long Length => _audioStreamImplementation.Length;
//
//     public override long Position
//     {
//         get => _audioStreamImplementation.Position;
//         set => _audioStreamImplementation.Position = value;
//     }
//
//     public override void Flush()
//     {
//         _audioStreamImplementation.Flush();
//     }
//
//     public override int Read(byte[] buffer, int offset, int count)
//     {
//         return _audioStreamImplementation.Read(buffer, offset, count);
//     }
//
//     public override long Seek(long offset, SeekOrigin origin)
//     {
//         return _audioStreamImplementation.Seek(offset, origin);
//     }
//
//     public override void SetLength(long value)
//     {
//         _audioStreamImplementation.SetLength(value);
//     }
//
//     public override void Write(byte[] buffer, int offset, int count)
//     {
//         _audioStreamImplementation.Write(buffer, offset, count);
//     }
//
//     public override ISampleProviderExtended CreateSampleProvider()
//     {
//         _sampleProvider = new WaveeAudioStream.WaveeSampleProvider(this);
//         return _sampleProvider;
//         //return _audioStreamImplementation.CreateSampleProvider();
//     }
// }