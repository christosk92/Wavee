using System.Net.Http.Headers;
using Eum.Logging;

namespace Eum.UI.Downloader;

public static class ImageDownloader
{
    private static HttpClient HttpClient
    {
        get;
    } = new HttpClient
    {
        MaxResponseContentBufferSize = 32 * 1024,
    };
    
    public static string DownloadPath { get; set; }

    public static async Task<string> DownloadAsync(this Uri url,
        
        CancellationToken ct = default)
    {
        await using var fs = File.Open(Path.Combine(DownloadPath, System.IO.Path.GetFileName(url.LocalPath)), FileMode.OpenOrCreate);
        if (fs.Length > 0) return fs.Name;
        var (stream, cl) =  await GetResponseStream(url, 0, ct);
        var BUF = System.Buffers.ArrayPool<byte>.Shared.Rent(32 * 1024);
        try
        {
            long bytesDownloaded = 0;

            System.Diagnostics.Trace.Listeners.Clear();

            using var timeoutToken = new CancellationTokenSource();
            using var linked_with_timeout =
                CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, ct);
            while (!ct.IsCancellationRequested &&
                   (cl - bytesDownloaded) > 0)
            {
                ct.ThrowIfCancellationRequested();
                linked_with_timeout.TryReset();
                timeoutToken.CancelAfter(TimeSpan.FromSeconds(2));
                try
                {
                    var x = await stream.ReadAsync(BUF, 0,
                        (int)Math.Min(BUF.Length,
                            cl - bytesDownloaded),
                        linked_with_timeout.Token);
                    bytesDownloaded += x;
                    if (x == 0)
                    {
                        S_Log.Instance.LogError("Unexpected EOF :: ");
                        throw new IOException("Unexpected EOF");
                        //throw new DownloadException(ErrorCode.Generic, "Unexpected EOF :: " + piece.Id);
                    }

                    await fs.WriteAsync(BUF.AsMemory(0, x),
                        // ReSharper disable once AccessToDisposedClosure
                        ct);
                }
                catch (TaskCanceledException c)
                {
                    if (timeoutToken.IsCancellationRequested)
                    {
                        throw new StreamTimeoutExcpetion();
                    }
                }
            }

            return fs.Name;
        }
        catch (OperationCanceledException taskCanceledException)
        {
            if (!ct.IsCancellationRequested)
            {
                throw;
            }

            await fs.DisposeAsync();
            File.Delete(fs.Name);
            return null;
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
            throw x;
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(BUF, false);
            await stream.DisposeAsync();
        }

    }


    private static async Task<(Stream Stream, long cL)> GetResponseStream(
        Uri url,
        long offset,
        CancellationToken ctCancellationToken = default)
    {
        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Get, url);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(offset, null);
        var response = await HttpClient.SendAsync(requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            ctCancellationToken);
        response.EnsureSuccessStatusCode();
        return  (await response.Content
            .ReadAsStreamAsync(ctCancellationToken)
            .ConfigureAwait(false), response.Content.Headers.ContentLength ?? -1);
    }
}

public class StreamTimeoutExcpetion : Exception
{
}