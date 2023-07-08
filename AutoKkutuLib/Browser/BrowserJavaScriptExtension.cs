using Serilog;
using System.Globalization;

namespace AutoKkutuLib.Browser;
public static class BrowserJavaScriptExtension
{
	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 주어진 리스트 타입으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static async Task<IList<T>?> EvaluateJavaScriptArrayAsync<T>(this BrowserBase browser, string script, T defaultElement, string? errorPrefix = null)
	{
		try
		{
			var collectionResult = await browser.EvaluateJavaScriptRawAsync(script);
			if (collectionResult == null)
				return null;
			return ((ICollection<object>)collectionResult).Select(e => (T)e).ToList();
		}
		catch (NullReferenceException)
		{
			return null;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, errorPrefix ?? "JavaScript execution error");
			return null;
		}
	}

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 문자열 타입으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static async Task<string?> EvaluateJavaScriptAsync(this BrowserBase browser, string script, string? defaultResult = null, string? errorPrefix = null)
	{
		try
		{
			return (await browser.EvaluateJavaScriptRawAsync(script))?.ToString() ?? defaultResult;
		}
		catch (NullReferenceException)
		{
			return defaultResult;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, errorPrefix ?? "JavaScript execution error");
			return defaultResult;
		}
	}

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 문자열 타입으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static string? EvaluateJavaScript(this BrowserBase browser, string script, string? defaultResult = null, string? errorPrefix = null)
		=> browser.EvaluateJavaScriptAsync(script, defaultResult, errorPrefix).Result;

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 <c>int</c>형으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static async Task<int> EvaluateJavaScriptIntAsync(this BrowserBase browser, string script, int defaultResult = -1, string? errorPrefix = null)
	{
		try
		{
			var interm = await browser.EvaluateJavaScriptRawAsync(script);
			return interm == null ? defaultResult : Convert.ToInt32(interm, CultureInfo.InvariantCulture);
		}
		catch (NullReferenceException)
		{
			return defaultResult;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, errorPrefix ?? "Failed to run script on site.");
			return defaultResult;
		}
	}

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 <c>int</c>형으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static int EvaluateJavaScriptInt(this BrowserBase browser, string script, int defaultResult = -1, string? errorPrefix = null)
		=> browser.EvaluateJavaScriptIntAsync(script, defaultResult, errorPrefix).Result;

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 <c>bool</c>형으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static async Task<bool> EvaluateJavaScriptBoolAsync(this BrowserBase browser, string script, bool defaultResult = false, string? errorMessage = null)
	{
		try
		{
			return Convert.ToBoolean(await browser.EvaluateJavaScriptRawAsync(script) ?? defaultResult);
		}
		catch (NullReferenceException)
		{
			return defaultResult;
		}
		catch (Exception ex)
		{
			Log.Warning(ex, errorMessage ?? "Failed to run script on site.");
			return defaultResult;
		}
	}

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 <c>bool</c>형으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static bool EvaluateJavaScriptBool(this BrowserBase browser, string script, bool defaultResult = false, string? errorMessage = null)
		=> browser.EvaluateJavaScriptBoolAsync(script, defaultResult, errorMessage).Result;
}
