namespace ConsoleApp.Events;

public class ProgressChangedEventArgs :  EventArgs
{
    public int Downloaded { get; init; }
    public int Total { get; init; }
}
