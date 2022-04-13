using CefSharp;
using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	internal partial class KkutuIoHandler : CommonHandler
	{
		private string _writeInputFuncName;
		private string _clickSubmitFuncName;

		public KkutuIoHandler(ChromiumWebBrowser browser) : base(browser)
		{
		}

		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?kkutu\\.io\\/\\?server.*$";

		public override string GetHandlerName() => "Kkutu.io Handler";

		public new void SendMessage(string input)
		{
			if (string.IsNullOrEmpty(_writeInputFuncName))
			{
				_writeInputFuncName = $"__{Utils.GenerateRandomString(new System.Random(), 64, true)}";

				Browser.EvaluateScriptAsync($@"
function {_writeInputFuncName}(input) {{
	var userMessages = document.querySelectorAll('#Middle > div.ChatBox.Product > div > input')
    var maxIndex = userMessages.length, index = 0;
    while (index <= maxIndex) {{
		var style = window.getComputedStyle(userMessages[index]);
        if (style.display != 'none' && style.visibility != 'hidden') {{
			userMessages[index].value = input;
            break;
        }}
		index++;
    }}
}}
");
			}

			if (string.IsNullOrEmpty(_clickSubmitFuncName))
			{
				_clickSubmitFuncName = $"__{Utils.GenerateRandomString(new System.Random(), 64, true)}";
				
				// https://stackoverflow.com/questions/6338217/get-a-css-value-with-javascript
				Browser.EvaluateScriptAsync($@"
function {_clickSubmitFuncName}() {{
	var buttons = document.querySelectorAll('#Middle > div.ChatBox.Product > div > button')
    var maxIndex = buttons.length, index = 0;
    while (index <= maxIndex) {{
		var style = window.getComputedStyle(buttons[index]);
        if (style.display != 'none' && style.visibility != 'hidden') {{
			buttons[index].click();
            break;
        }}
		index++;
    }}
}}
");
			}

			EvaluateJS($"{_writeInputFuncName}('{input}')");
			EvaluateJS($"{_clickSubmitFuncName}()");
		}
	}
}
