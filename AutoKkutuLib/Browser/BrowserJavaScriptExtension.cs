using System.Collections.Immutable;
using System.Globalization;

namespace AutoKkutuLib.Browser;
public static class BrowserJavaScriptExtension
{
	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 주어진 리스트 타입으로 가져옵니다.
	/// 만약 리스트 타입 캐스팅 도중 오류가 발생한다면, <paramref name="defaultElement"/>를 대신 사용합니다.
	/// 만약 실행 도중 오류가 발생하거나, 주어진 <paramref name="script"/>가 <c>undefined</c> 또는 <c>null</c>을 반환한다면 빈 리스트가 반환됩니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static async ValueTask<IImmutableList<T>> EvaluateJavaScriptArrayAsync<T>(this BrowserBase browser, string script, T defaultElement, string? errorPrefix = null) where T : class
	{
		try
		{
			var collectionResult = await browser.EvaluateJavaScriptRawAsync(script);
			if (collectionResult != null)
				return ((ICollection<object>)collectionResult).Select(e => (e as T) ?? defaultElement).ToImmutableList();
		}
		catch (NullReferenceException)
		{
			// ignored
		}
		catch (Exception ex)
		{
			LibLogger.Warn(nameof(BrowserJavaScriptExtension), ex, errorPrefix ?? "JavaScript execution error");
		}

		return ImmutableList<T>.Empty;
	}

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 문자열 타입으로 가져옵니다.
	/// 만약 실행 도중 오류가 발생하거나, 주어진 <paramref name="script"/>가 <c>undefined</c> 또는 <c>null</c>을 반환한다면 <paramref name="defaultResult"/>를 대신 반환합니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	public static async ValueTask<string> EvaluateJavaScriptAsync(this BrowserBase browser, string script, string defaultResult = "", string? errorPrefix = null)
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
			LibLogger.Warn(nameof(BrowserJavaScriptExtension), ex, errorPrefix ?? "JavaScript execution error");
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
	[Obsolete("EvaluateJavaScriptAsync()를 대신 사용하는 것이 좋습니다")]
	public static string EvaluateJavaScript(this BrowserBase browser, string script, string defaultResult = "", string? errorPrefix = null)
		=> browser.EvaluateJavaScriptAsync(script, defaultResult, errorPrefix).AsTask().Result;

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 <c>int</c>형으로 가져옵니다.
	/// 만약 실행 도중 오류가 발생하거나, 주어진 <paramref name="script"/>가 <c>undefined</c> 또는 <c>null</c>을 반환한다면 <paramref name="defaultResult"/>를 대신 반환합니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static async ValueTask<int> EvaluateJavaScriptIntAsync(this BrowserBase browser, string script, int defaultResult = -1, string? errorPrefix = null)
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
			LibLogger.Warn(nameof(BrowserJavaScriptExtension), ex, errorPrefix ?? "Failed to run script on site.");
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
	[Obsolete("EvaluateJavaScriptIntAsync()를 대신 사용하는 것이 좋습니다")]
	public static int EvaluateJavaScriptInt(this BrowserBase browser, string script, int defaultResult = -1, string? errorPrefix = null)
		=> browser.EvaluateJavaScriptIntAsync(script, defaultResult, errorPrefix).AsTask().Result;

	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 <c>bool</c>형으로 가져옵니다.
	/// 만약 실행 도중 오류가 발생하거나, 주어진 <paramref name="script"/>가 <c>undefined</c> 또는 <c>null</c>을 반환한다면 <paramref name="defaultResult"/>를 대신 반환합니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	public static async ValueTask<bool> EvaluateJavaScriptBoolAsync(this BrowserBase browser, string script, bool defaultResult = false, string? errorMessage = null)
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
			LibLogger.Warn(nameof(BrowserJavaScriptExtension), ex, errorMessage ?? "Failed to run script on site.");
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
	/// <summary>
	/// 주어진 <paramref name="script"/>를 브라우저에서 실행하고, 결과를 <c>int</c>형으로 가져옵니다.
	/// </summary>
	/// <param name="script">실행할 JavaScript</param>
	/// <param name="defaultResult">기본 결과 값. 만약 실행 도중 오류와 같은 예외적인 상황이 발생한다면 이 값이 반환됩니다.</param>
	/// <param name="errorPrefix">실행 도중 예외가 발생한다면 사용될 예외 설명</param>
	/// <returns></returns>
	[Obsolete("EvaluateJavaScriptBool()를 대신 사용하는 것이 좋습니다")]
	public static bool EvaluateJavaScriptBool(this BrowserBase browser, string script, bool defaultResult = false, string? errorMessage = null)
		=> browser.EvaluateJavaScriptBoolAsync(script, defaultResult, errorMessage).AsTask().Result;
}
