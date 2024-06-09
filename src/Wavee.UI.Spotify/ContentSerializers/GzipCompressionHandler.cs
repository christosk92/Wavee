using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wavee.UI.Spotify.ContentSerializers;

public class GzipCompressionHandler : DelegatingHandler
{
    public GzipCompressionHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            var originalContentStream = await request.Content.ReadAsStreamAsync(cancellationToken);
            var compressedContentStream = new MemoryStream();

            await using (var gzipStream = new GZipStream(compressedContentStream, CompressionMode.Compress, leaveOpen: true))
            {
                await originalContentStream.CopyToAsync(gzipStream, cancellationToken);
            }

            compressedContentStream.Seek(0, SeekOrigin.Begin);
            request.Content = new StreamContent(compressedContentStream);
            request.Content.Headers.ContentEncoding.Add("gzip");
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

public class GzipDecompressionHandler : DelegatingHandler
{
    public GzipDecompressionHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content.Headers.ContentEncoding.Contains("gzip"))
        {
            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            response.Content = new StreamContent(new GZipStream(contentStream, CompressionMode.Decompress));
            foreach (var header in response.Content.Headers)
            {
                response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        return response;
    }
}

public class CompressedContent : HttpContent
{
    private readonly HttpContent _originalContent;
    private readonly CompressionMode _compressionMode;

    public CompressedContent(HttpContent content, CompressionMode compressionMode)
    {
        _originalContent = content ?? throw new ArgumentNullException(nameof(content));
        _compressionMode = compressionMode;

        foreach (var header in _originalContent.Headers)
        {
            Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (_compressionMode == CompressionMode.Compress)
        {
            Headers.ContentEncoding.Add("gzip");
        }
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        using (var gzipStream = new GZipStream(stream, _compressionMode, leaveOpen: true))
        {
            await _originalContent.CopyToAsync(gzipStream);
        }
    }


    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }

    protected override async Task<Stream> CreateContentReadStreamAsync()
    {
        var stream = await _originalContent.ReadAsStreamAsync();
        if (_compressionMode == CompressionMode.Decompress)
        {
            return new GZipStream(stream, CompressionMode.Decompress);
        }

        return stream;
    }
}