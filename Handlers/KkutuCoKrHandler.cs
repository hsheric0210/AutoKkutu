using CefSharp;
using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	internal partial class KkutuCoKrHandler : CommonHandler
	{
		private string _writeInputFuncName;
		private string _clickSubmitFuncName;
		
		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?kkutu\\.co\\.kr\\/.*\\/game.*$";

		public override string GetHandlerName() => "Kkutu.co.kr Handler";

		public override void SendMessage(string input)
		{
			if (string.IsNullOrEmpty(_writeInputFuncName) || EvaluateJSBool($"typeof {_writeInputFuncName} != 'function'"))
			{
				_writeInputFuncName = $"__{Utils.GenerateRandomString(64, true)}";

				if (EvaluateJSReturnError($@"
function {_writeInputFuncName}(input) {{
	var userMessages = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > input')
    var maxIndex = userMessages.length, index = 0;
    while (index < maxIndex) {{
        if (window.getComputedStyle(userMessages[index]).display != 'none') {{
			userMessages[index].value = input;
            break;
        }}
		index++;
    }}
}}
", out string error))
					GetLogger().ErrorFormat("Failed to register writeInputFunc: {0}", error);
				else
					GetLogger().Info($"Register writeInputFunc: {_writeInputFuncName}()");
			}

			if (string.IsNullOrEmpty(_clickSubmitFuncName) || EvaluateJSBool($"typeof {_clickSubmitFuncName} != 'function'"))
			{
				_clickSubmitFuncName = $"__{Utils.GenerateRandomString(64, true)}";

				// https://stackoverflow.com/questions/6338217/get-a-css-value-with-javascript
				if (EvaluateJSReturnError($@"
function {_clickSubmitFuncName}() {{
	var buttons = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > button')
    var maxIndex = buttons.length, index = 0;
    while (index < maxIndex) {{
        if (window.getComputedStyle(buttons[index]).display != 'none') {{
			buttons[index].click();
            break;
        }}
		index++;
    }}
}}
", out string error))
					GetLogger().ErrorFormat("Failed to register clickSubmitFunc: {0}", error);
				else
					GetLogger().Info($"Register clickSubmitFunc: {_clickSubmitFuncName}()");
			}

			EvaluateJS($"{_writeInputFuncName}('{input}')");
			EvaluateJS($"{_clickSubmitFuncName}()");
		}
	}
}
