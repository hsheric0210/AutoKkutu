using Serilog;
using System.Globalization;

namespace AutoKkutuLib;

public class JsEvaluator
{
	private readonly IKkutuBrowser browser;

	public JsEvaluator(IKkutuBrowser browser) => this.browser = browser;

	private object? EvaluateJSInternal(string javaScript, object? defaultResult) => browser.EvaluateScriptAsync(javaScript).Result.Result ?? defaultResult;

	/// <summary>
	/// Execute the javascript and return the <u>Error Message</u>
	/// </summary>
	/// <param name="javaScript">Javascript script to execute in browser main frame</param>
	/// <param name="error">Error message if available. Empty if not.</param>
	/// <returns>true if error occurred, false otherwise.</returns>
	public bool EvaluateJSReturnError(string javaScript, out string error)
	{
		error = "";
		JSResponse task = browser.EvaluateScriptAsync(javaScript).Result;
		if (!task.Success)
			error = task.Message;
		return !task.Success;
	}

	public string EvaluateJS(string javaScript, string defaultResult = " ", string? errorMessage = null)
	{
		try
		{
			return EvaluateJSInternal(javaScript, defaultResult)?.ToString() ?? defaultResult;
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

	public int EvaluateJSInt(string javaScript, int defaultResult = -1, string? errorMessage = null)
	{
		try
		{
			var internalResult = EvaluateJSInternal(javaScript, defaultResult);
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

	public bool EvaluateJSBool(string javaScript, bool defaultResult = false, string? errorMessage = null)
	{
		try
		{
			return Convert.ToBoolean(EvaluateJSInternal(javaScript, defaultResult), CultureInfo.InvariantCulture);
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
