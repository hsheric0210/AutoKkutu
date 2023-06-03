namespace AutoKkutuLib.Browser.Events;

public class PageLoadedEventArgs : EventArgs
{
	public PageLoadedEventArgs(string url) => Url = url;

	public string Url { get; }
}
