namespace AutoKkutuLib.Browser.Events;

public class PageErrorEventArgs : EventArgs
{
	public PageErrorEventArgs(string errorText, string failedUrl)
	{
		ErrorText = errorText;
		FailedUrl = failedUrl;
	}

	public string FailedUrl { get; }
	public string ErrorText { get; }
}
