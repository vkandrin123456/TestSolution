namespace System
{
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
}
