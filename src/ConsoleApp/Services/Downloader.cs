using ConsoleApp.Events;
using Microsoft.Extensions.Logging;

namespace ConsoleApp.Services;

public class Downloader : IDownloader
{
    private readonly ILogger<Downloader> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly object synObj = new();

    public Downloader(
        ILogger<Downloader> logger,
        IHttpClientFactory clientFactory
        )
    {
        _logger = logger;
        _clientFactory = clientFactory;
    }

    public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

    public event AsyncEventHandler<FileDownloadedEventArgs> FileDownloadedAsync;

    protected async virtual Task OnFileDownloadedAsync(FileDownloadedEventArgs args)
    {
        await FileDownloadedAsync?.Invoke(this, args);
    }

    protected virtual void OnProgressChanged(ProgressChangedEventArgs args)
    {
        ProgressChanged?.Invoke(this, args);
    }

    public Task DownloadAsync(
        ICollection<string> urls,
        string targetDirectory,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default
        )
    {
        Directory.CreateDirectory(targetDirectory);
        var ctx = new DownloadContext(urls.ToArray(), targetDirectory);

        return Parallel.ForEachAsync(
            urls.Select(u => new Downloadable(u, ctx)),
            new ParallelOptions()
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            },
            DownloadFileAsync);
    }

    private async ValueTask DownloadFileAsync(Downloadable downloadable, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _clientFactory.CreateClient(Consts.HttpClientName);
            using var response = await client.GetAsync(downloadable.Url, cancellationToken);
            using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            //await client.GetStreamAsync(downloadable.Url, cancellationToken);
            using var output = new FileStream(
                                GetFilePath(response.Content.Headers.ContentDisposition?.FileName, ref downloadable),
                                FileMode.Create);

            await input.CopyToAsync(output, cancellationToken);

            lock (synObj)
            {
                OnProgressChanged(
                    new ProgressChangedEventArgs
                    {
                        Total = downloadable.Context.Urls.Length,
                        Downloaded = Interlocked.Increment(ref downloadable.Context.Downloaded)
                    });
            }
            await OnFileDownloadedAsync(new FileDownloadedEventArgs
            {
                Url = downloadable.Url,
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File download failed {@details}",
                new
                {
                    url = downloadable.Url
                }
            );
        }
    }

    private string GetFilePath(string filename, ref Downloadable downloadable)
    {
        if (filename == null)
        {
            var uri = new Uri(downloadable.Url);
            var uriPath = uri.GetLeftPart(UriPartial.Path);
            filename = uriPath.Substring(uriPath.LastIndexOf('/') + 1);
        }
        return Path.Combine(downloadable.Context.TargetDirectory, filename);
    }

    private class DownloadContext
    {
        public readonly string[] Urls;
        public readonly string TargetDirectory;
        public int Downloaded;

        public DownloadContext(string[] urls, string targetDirectory)
        {
            Urls = urls;
            TargetDirectory = targetDirectory;
        }
    }

    private readonly struct Downloadable
    {
        public readonly string Url;
        public readonly DownloadContext Context;
        public Downloadable(string url, DownloadContext context)
        {
            Url = url;
            Context = context;
        }
    }
}
