namespace AutoKkutuLib.Browser;

// TODO: 사전 검색 기능 싹 다갈아엎고 DomHandler.cs, Game.cs에 직접적으로 추가하여 엄연한 하나의 공식 기능으로 만들기.
public static class OnlineDictionaryCheckExtension
{
	public static bool IsDictionaryAvailable(this BrowserBase browser)
	{
		return !string.IsNullOrWhiteSpace(browser.EvaluateJavaScript("document.getElementById('dict-output').style"));

		// FIXME: Replace with event
		//if (string.IsNullOrWhiteSpace(jsEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
		//	MessageBox.Show("끄투 사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", "Word online verification", MessageBoxButton.OK, MessageBoxImage.Warning);
		//return false;
	}

	/// <summary>
	/// Check if the word is available in the current server using the official kkutu dictionary feature.
	/// </summary>
	/// <param name="word">The word to check</param>
	/// <returns>True if existence is verified, false otherwise.</returns>
	public static bool VerifyWordOnline(this BrowserBase browser, string word)
	{
		LibLogger.Info(nameof(OnlineDictionaryCheckExtension), I18n.BatchJob_CheckOnline, word);

		// Enter the word to dictionary search field
		browser.EvaluateJavaScript($"document.getElementById('dict-input').value = '{word}'");

		// Click search button
		browser.EvaluateJavaScript("document.getElementById('dict-search').click()");

		// Wait for response
		Thread.Sleep(1500);

		// Query the response
		var result = browser.EvaluateJavaScript("document.getElementById('dict-output').innerHTML");
		LibLogger.Info(nameof(OnlineDictionaryCheckExtension), I18n.BatchJob_CheckOnline_Response, result);
		if (string.IsNullOrWhiteSpace(result) || string.Equals(result, "404: 유효하지 않은 단어입니다.", StringComparison.OrdinalIgnoreCase))
		{
			LibLogger.Warn(nameof(OnlineDictionaryCheckExtension), I18n.BatchJob_CheckOnline_NotFound, word);
			return false;
		}
		else if (string.Equals(result, "검색 중", StringComparison.OrdinalIgnoreCase))
		{
			LibLogger.Warn(nameof(OnlineDictionaryCheckExtension), I18n.BatchJob_CheckOnline_InvalidResponse);
			return browser.VerifyWordOnline(word); // retry
		}
		else
		{
			LibLogger.Info(nameof(OnlineDictionaryCheckExtension), I18n.BatchJob_CheckOnline_Found, word);
			return true;
		}
	}
}
