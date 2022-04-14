using CefSharp;
using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	// Fixme: Detected by their cheat detection

	internal partial class KkutuIoHandler : CommonHandler
	{
		private string _parseExtraVisibleStyleTags;
		private string _writeInputFuncName;
		private string _clickSubmitFuncName;

		public KkutuIoHandler(ChromiumWebBrowser browser) : base(browser)
		{
		}

		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?kkutu\\.io\\/\\?server=.*$";

		public override string GetHandlerName() => "Kkutu.io Handler";

		public new void SendMessage(string input)
		{
			if (string.IsNullOrEmpty(_parseExtraVisibleStyleTags) || EvaluateJSBool($"typeof {_parseExtraVisibleStyleTags} != 'function'"))
			{
				_parseExtraVisibleStyleTags = $"__{Utils.GenerateRandomString(new System.Random(), 64, true)}";

				// https://stackoverflow.com/a/14865690
				Browser.EvaluateScriptAsync($@"
function {_parseExtraVisibleStyleTags}() {{
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
");
			}

			if (string.IsNullOrEmpty(_writeInputFuncName) || EvaluateJSBool($"typeof {_writeInputFuncName} != 'function'"))
			{
				_writeInputFuncName = $"__{Utils.GenerateRandomString(new System.Random(), 64, true)}";

				Browser.EvaluateScriptAsync($@"
function {_writeInputFuncName}(input) {{
	var talks = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > input'), maxTalks=talks.length;
	var visible = {_parseExtraVisibleStyleTags}(), nVisible = visible.length;
	for (let index=0;index<maxTalks;index++) {{
		for (let index2=0;index2<nVisible;index2++) {{
			if (talks[index].id == visible[index2]) {{
				talks[index].value = input;
				break;
			}}
		}}
    }}
}};
");
			}

			if (string.IsNullOrEmpty(_clickSubmitFuncName) || EvaluateJSBool($"typeof {_clickSubmitFuncName} != 'function'"))
			{
				_clickSubmitFuncName = $"__{Utils.GenerateRandomString(new System.Random(), 64, true)}";
				
				// https://stackoverflow.com/questions/6338217/get-a-css-value-with-javascript
				Browser.EvaluateScriptAsync($@"
function {_clickSubmitFuncName}() {{
	var buttons = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > button'), maxButtons=buttons.length;
	var visible = {_parseExtraVisibleStyleTags}(), nVisible = visible.length;
	for (let index=0;index<maxButtons;index++) {{
		for (let index2=0;index2<nVisible;index2++) {{
			if (buttons[index].id == visible[index2]) {{
				buttons[index].click();
				break;
			}}
		}}
    }}
}}
");
			}

			EvaluateJS($"{_writeInputFuncName}('{input}')");
			//EvaluateJS($"{_clickSubmitFuncName}()");
		}
	}
}
