using ConsoleApp.Events;

namespace ConsoleApp.Services;

public interface IDownloader
{
    Task DownloadAsync(
        ICollection<string> urls,
        string targetDirectory,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default);

    event EventHandler<ProgressChangedEventArgs> ProgressChanged;

    event AsyncEventHandler<FileDownloadedEventArgs> FileDownloadedAsync;
}
