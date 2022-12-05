using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Database.Extension;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoKkutuLib.Utils;

public static class OnlineVerifyExtension
{
	/// <summary>
	/// Check if the word is available in the current server using the official kkutu dictionary feature.
	/// </summary>
	/// <param name="word">The word to check</param>
	/// <returns>True if existence is verified, false otherwise.</returns>
	public static bool VerifyWordOnline(this string word)
	{
		Log.Information(I18n.BatchJob_CheckOnline, word);

		// Enter the word to dictionary search field
		JSEvaluator.EvaluateJS($"document.getElementById('dict-input').value = '{word}'");

		// Click search button
		JSEvaluator.EvaluateJS("document.getElementById('dict-search').click()");

		// Wait for response
		Thread.Sleep(1500);

		// Query the response
		var result = JSEvaluator.EvaluateJS("document.getElementById('dict-output').innerHTML");
		Log.Information(I18n.BatchJob_CheckOnline_Response, result);
		if (string.IsNullOrWhiteSpace(result) || string.Equals(result, "404: 유효하지 않은 단어입니다.", StringComparison.OrdinalIgnoreCase))
		{
			Log.Warning(I18n.BatchJob_CheckOnline_NotFound, word);
			return false;
		}
		else if (string.Equals(result, "검색 중", StringComparison.OrdinalIgnoreCase))
		{
			Log.Warning(I18n.BatchJob_CheckOnline_InvalidResponse);
			return word.VerifyWordOnline(); // retry
		}
		else
		{
			Log.Information(I18n.BatchJob_CheckOnline_Found, word);
			return true;
		}
	}
}
