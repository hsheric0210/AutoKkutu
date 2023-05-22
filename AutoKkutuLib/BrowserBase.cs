using AutoKkutuLib.Game;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib;

public abstract class BrowserBase
{
	/// <summary>
	/// type is 'object' to prevent WPF to dependency. Please cast to <see cref="Control"/> when using.
	/// May be null if the WPF frame is not available
	/// </summary>
	public abstract object? BrowserControl { get; }
	public EventHandler<PageLoadedEventArgs> PageLoaded;
	public EventHandler<PageErrorEventArgs> PageError;

	public abstract void LoadFrontPage();
	public abstract void Load(string url);
	public abstract void ShowDevTools();
	public abstract void ExecuteJavaScript(string script, string? errorMessage = null);
	public abstract Task<JavaScriptCallback> EvaluateJavaScriptAsync(string script);

	private object? EvaluateJavaScriptSync(string javaScript, object? defaultResult)
	{
		Task<JavaScriptCallback> task = EvaluateJavaScriptAsync(javaScript);
		if (!task.Wait(TimeSpan.FromSeconds(5)))
			throw new TimeoutException("JavaScript execution time-out");
		return task.Result.Result ?? defaultResult;
	}

	/// <summary>
	/// Execute the javascript and return the <u>Error Message</u>
	/// </summary>
	/// <param name="javaScript">Javascript script to execute in browser main frame</param>
	/// <param name="error">Error message if available. Empty if not.</param>
	/// <returns>true if error occurred, false otherwise.</returns>
	public bool EvaluateJSAndGetError(string javaScript, out string error)
	{
		error = "";
		Task<JavaScriptCallback> task = EvaluateJavaScriptAsync(javaScript);
		if (!task.Wait(TimeSpan.FromSeconds(5)))
			throw new TimeoutException("JavaScript execution time-out");
		JavaScriptCallback result = task.Result;
		if (!result.Success)
			error = result.Message;
		return !result.Success;
	}

	public string EvaluateJavaScript(string javaScript, string defaultResult = "", string? errorMessage = null)
	{
		try
		{
			return EvaluateJavaScriptSync(javaScript, defaultResult)?.ToString() ?? defaultResult;
		}
		catch (NullReferenceException)
		{
			return defaultResult;
		}
		catch (Exception ex)
		{
			Log.Error(ex, errorMessage ?? "JavaScript execution error");
			return defaultResult;
		}
	}

	public int EvaluateJavaScriptInt(string javaScript, int defaultResult = -1, string? errorMessage = null)
	{
		try
		{
			var internalResult = EvaluateJavaScriptSync(javaScript, defaultResult);
			return internalResult == null ? defaultResult : Convert.ToInt32(internalResult, CultureInfo.InvariantCulture);
		}
		catch (NullReferenceException)
		{
			return defaultResult;
		}
		catch (Exception ex)
		{
			Log.Error(ex, errorMessage ?? "Failed to run script on site.");
			return defaultResult;
		}
	}

	public bool EvaluateJavaScriptBool(string javaScript, bool defaultResult = false, string? errorMessage = null)
	{
		try
		{
			return Convert.ToBoolean(EvaluateJavaScriptSync(javaScript, defaultResult), CultureInfo.InvariantCulture);
		}
		catch (NullReferenceException)
		{
			return defaultResult;
		}
		catch (Exception ex)
		{
			Log.Error(ex, errorMessage ?? "Failed to run script on site.");
			return defaultResult;
		}
	}
}

public record JavaScriptCallback(string Message, bool Success, object? Result);

public class PageLoadedEventArgs : EventArgs
{
	public PageLoadedEventArgs(string url) => Url = url;

	public string Url { get; }
}

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