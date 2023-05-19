namespace ConsoleApp.Events;

public class FileDownloadedEventArgs : EventArgs
{
    public string Url { get; init; }
}
