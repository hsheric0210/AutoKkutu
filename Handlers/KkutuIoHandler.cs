using CefSharp;
using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	// Fixme: Detected by their cheat detection
	// 단어를 '글자 별로 하나하나' 입력하는 기능(CEF의 KeyEvent 활용), 딜레이 강제 적용(한 글자 당 100ms 이상)을 해야지만 제대로 우회할 수 있다.
	// https://github.com/horyu1234/KKuTu/blob/91118d0db5a2cc35147c86bdac7ca8df9bdf0f4f/Server/lib/Web/lib/kkutu/body.js#L95

	internal partial class KkutuIoHandler : CommonHandler
	{
		private string _parseExtraVisibleStyleTagsFuncName;
		private string _writeInputFuncName;
		private string _clickSubmitFuncName;

		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?kkutu\\.io\\/\\?server=.*$";

		public override string GetHandlerName() => "Kkutu.io Handler";

		public override void SendMessage(string input)
		{
			if (string.IsNullOrEmpty(_parseExtraVisibleStyleTagsFuncName) || EvaluateJSBool($"typeof {_parseExtraVisibleStyleTagsFuncName} != 'function'"))
			{
				_parseExtraVisibleStyleTagsFuncName = $"__{Utils.GenerateRandomString(64, true, new System.Random())}";

				// https://stackoverflow.com/a/14865690
				if (EvaluateJSReturnError($@"
function {_parseExtraVisibleStyleTagsFuncName}() {{
	var styles = document.querySelectorAll('style');
	var maxIndex = styles.length, index = 0;
	var visibleStyles = [];
    while (index < maxIndex) {{
		var doc = document.implementation.createHTMLDocument(""),
            styleElement = document.createElement('style');

		styleElement.textContent = styles[index].textContent;
		doc.body.appendChild(styleElement);

		var css = styleElement.sheet.cssRules[0];
		if (css.selectorText[0] == '#' && css.style.display != 'none' && css.style.visibility != 'hidden')
		{{
			visibleStyles.push(css.selectorText.substring(1));
		}}
		index++;
	}}
	return visibleStyles;
}};
", out string error))
					GetLogger().ErrorFormat("Failed to register parseExtraVisibleStyleTagsFunc: {0}", error);
				else
					GetLogger().Info($"Register parseExtraVisibleStyleTagsFunc: {_parseExtraVisibleStyleTagsFuncName}()");
			}

			if (string.IsNullOrEmpty(_writeInputFuncName) || EvaluateJSBool($"typeof {_writeInputFuncName} != 'function'"))
			{
				_writeInputFuncName = $"__{Utils.GenerateRandomString(64, true, new System.Random())}";

				if (EvaluateJSReturnError($@"
function {_writeInputFuncName}(input) {{
	var talks = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > input'), maxTalks=talks.length;
	var visible = {_parseExtraVisibleStyleTagsFuncName}(), nVisible = visible.length;
	for (let index=0;index<maxTalks;index++) {{
		for (let index2=0;index2<nVisible;index2++) {{
			if (talks[index].id == visible[index2]) {{
				talks[index].value = input;
				break;
			}}
		}}
    }}
}};
", out string error))
					GetLogger().ErrorFormat("Failed to register writeInputFunc: {0}", error);
				else
					GetLogger().Info($"Register writeInputFunc: {_writeInputFuncName}()");
			}

			if (string.IsNullOrEmpty(_clickSubmitFuncName) || EvaluateJSBool($"typeof {_clickSubmitFuncName} != 'function'"))
			{
				_clickSubmitFuncName = $"__{Utils.GenerateRandomString(64, true, new System.Random())}";
				
				// https://stackoverflow.com/questions/6338217/get-a-css-value-with-javascript
				if (EvaluateJSReturnError($@"
function {_clickSubmitFuncName}() {{
	var buttons = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > button'), maxButtons=buttons.length;
	var visible = {_parseExtraVisibleStyleTagsFuncName}(), nVisible = visible.length;
	for (let index=0;index<maxButtons;index++) {{
		for (let index2=0;index2<nVisible;index2++) {{
			if (buttons[index].id == visible[index2]) {{
				buttons[index].click();
				break;
			}}
		}}
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
