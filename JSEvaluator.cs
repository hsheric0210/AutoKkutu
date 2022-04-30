using CefSharp;
using CefSharp.Wpf;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public class JSEvaluator
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof(JSEvaluator));
		private static ChromiumWebBrowser Browser;

		public static void RegisterBrowser(ChromiumWebBrowser browser)
		{
			if (Browser != null)
				throw new AccessViolationException("Browser is already registered");
			Browser = browser;
		}

		private static object EvaluateJSInternal(string javaScript, object defaultResult)
		{
			if (!Browser.CanExecuteJavascriptInMainFrame)
				return defaultResult;

			using (var frame = Browser.GetMainFrame())
			{
				if (frame != null)
					return frame.EvaluateScriptAsync(javaScript).Result.Result ?? defaultResult;
			}
			return defaultResult;
		}

		/// <summary>
		/// Execute the javascript and return the <u>Error Message</u>
		/// </summary>
		/// <param name="javaScript">Javascript script to execute in browser main frame</param>
		/// <param name="error">Error message if available. Empty if not.</param>
		/// <returns>true if error occurred, false otherwise.</returns>
		public static bool EvaluateJSReturnError(string javaScript, out string error)
		{
			if (!Browser.CanExecuteJavascriptInMainFrame)
			{
				error = "Browser is not prepared";
				return true;
			}

			using (var frame = Browser.GetMainFrame())
			{
				if (frame != null)
				{
					error = frame.EvaluateScriptAsync(javaScript).Result.Message;
					return !string.IsNullOrWhiteSpace(error);
				}
			}
			error = "Main frame is null";
			return true;
		}

		public static string EvaluateJS(string javaScript, string defaultResult = " ", ILog logger = null)
		{
			try
			{
				return EvaluateJSInternal(javaScript, defaultResult).ToString();
			}
			catch (NullReferenceException)
			{
				return defaultResult;
			}
			catch (Exception ex)
			{
				(logger ?? Logger).Error("Failed to run script on site.", ex);
				return defaultResult;
			}
		}

		public static int EvaluateJSInt(string javaScript, int defaultResult = -1, ILog logger = null)
		{
			try
			{
				return Convert.ToInt32(EvaluateJSInternal(javaScript, defaultResult));
			}
			catch (NullReferenceException)
			{
				return defaultResult;
			}
			catch (Exception ex)
			{
				(logger ?? Logger).Error("Failed to run script on site.", ex);
				return defaultResult;
			}
		}

		public static bool EvaluateJSBool(string javaScript, bool defaultResult = false, ILog logger = null)
		{
			try
			{
				return Convert.ToBoolean(EvaluateJSInternal(javaScript, defaultResult));
			}
			catch (NullReferenceException)
			{
				return defaultResult;
			}
			catch (Exception ex)
			{
				(logger ?? Logger).Error("Failed to run script on site.", ex);
				return defaultResult;
			}
		}
	}
}
