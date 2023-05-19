namespace ConsoleApp.Configs;

public class DownloaderConfig
{
    public bool SaveState { get; set; } 
    public int MaxParallelism { get; set; }
    public string DownloadDirecory { get; set; }
}
