﻿namespace AutoKkutuLib.Handlers;

// TODO: Detected by their cheat detection
// 단어를 '글자 별로 하나하나' 입력하는 기능(CEF의 KeyEvent 활용), 딜레이 강제 적용(한 글자 당 100ms 이상)을 해야지만 제대로 우회할 수 있다.
// https://github.com/horyu1234/KKuTu/blob/91118d0db5a2cc35147c86bdac7ca8df9bdf0f4f/Server/lib/Web/lib/kkutu/body.js#L95

internal partial class KkutuIoHandler : AbstractHandler
{
	private const string ParseExtraVisibilityStyleTagsFunc = "ParseExtraVisibilityStyleTagsFunc";

	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] { new Uri("https://kkutu.io/") };

	public override string HandlerName => "Kkutu.io Handler";

	public KkutuIoHandler(JsEvaluator jsEvaluator) : base(jsEvaluator)
	{
	}

	public override void UpdateChat(string input)
	{
		RegisterJSFunction(ParseExtraVisibilityStyleTagsFunc, "", @"
var styles = document.querySelectorAll('style');
var maxIndex = styles.length, index = 0;
var visibleStyles = [];
while (index < maxIndex) {
	var doc = document.implementation.createHTMLDocument(""),
        styleElement = document.createElement('style');

	styleElement.textContent = styles[index].textContent;
	doc.body.appendChild(styleElement);

	var css = styleElement.sheet.cssRules[0];
	if (css.selectorText[0] == '#' && css.style.display != 'none' && css.style.visibility != 'hidden')
	{
		visibleStyles.push(css.selectorText.substring(1));
	}
	index++;
}
return visibleStyles;
");

		RegisterJSFunction(WriteInputFunc, "input", $@"
var talks = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > input'), maxTalks=talks.length;
var visible = {GetRegisteredJSFunctionName(ParseExtraVisibilityStyleTagsFunc)}(), nVisible = visible.length;
for (let index=0;index<maxTalks;index++) {{
	for (let index2=0;index2<nVisible;index2++) {{
		if (talks[index].id == visible[index2]) {{
			talks[index].value = input;
			break;
		}}
	}}
}}
");

		EvaluateJS($"{GetRegisteredJSFunctionName(WriteInputFunc)}('{input}')");
	}

	public override void ClickSubmit()
	{
		RegisterJSFunction(ParseExtraVisibilityStyleTagsFunc, "", @"
var styles = document.querySelectorAll('style');
var maxIndex = styles.length, index = 0;
var visibleStyles = [];
while (index < maxIndex) {
	var doc = document.implementation.createHTMLDocument(""),
        styleElement = document.createElement('style');

	styleElement.textContent = styles[index].textContent;
	doc.body.appendChild(styleElement);

	var css = styleElement.sheet.cssRules[0];
	if (css.selectorText[0] == '#' && css.style.display != 'none' && css.style.visibility != 'hidden')
	{
		visibleStyles.push(css.selectorText.substring(1));
	}
	index++;
}
return visibleStyles;
");

		RegisterJSFunction(ClickSubmitFunc, "", $@"
var buttons = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > button'), maxButtons=buttons.length;
var visible = {GetRegisteredJSFunctionName(ParseExtraVisibilityStyleTagsFunc)}(), nVisible = visible.length;
for (let index=0;index<maxButtons;index++) {{
	for (let index2=0;index2<nVisible;index2++) {{
		if (buttons[index].id == visible[index2]) {{
			buttons[index].click();
			break;
		}}
	}}
}}
");

		EvaluateJS($"{GetRegisteredJSFunctionName(ClickSubmitFunc)}()");
	}
}