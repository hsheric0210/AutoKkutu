namespace AutoKkutuLib.Handlers.JavaScript.Handlers;

// TODO: Detected by their cheat detection
// 단어를 '글자 별로 하나하나' 입력하는 기능(CEF의 KeyEvent 활용), 딜레이 강제 적용(한 글자 당 100ms 이상)을 해야지만 제대로 우회할 수 있다.
// https://github.com/horyu1234/KKuTu/blob/91118d0db5a2cc35147c86bdac7ca8df9bdf0f4f/Server/lib/Web/lib/kkutu/body.js#L95

/// <summary>
/// 끄투리오는 CSS 대신 HTML Header에 display: none 스타일을 걸어 놓았습니다. 이 경우, 굳이 오버헤드 큰 window.getComputedStyle()를 안 써도 됩니다.
/// </summary>

internal class OptimizedBypassHandler : JavaScriptHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] { new Uri("https://kkutu.io/") }; // 여기는 jjo-display도 페이크 만듦 + 100ms 이내에 4글자 이상 입력 시 detect

	public override string HandlerName => "Optimized Fake-element Bypassing Handler";

	public OptimizedBypassHandler(BrowserBase jsEvaluator) : base(jsEvaluator)
	{
	}

	public override void RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		//ParseExtraVisibilityStyleTagsFunc
		RegisterJavaScriptFunction(alreadyRegistered, 99, "", @"
let styles = document.querySelectorAll('style');
let maxIndex = styles.length, index = 0;
let visibleStyles = [];
while (index < maxIndex) {
	let doc = document.implementation.createHTMLDocument('');
	let styleElement = document.createElement('style');
	let content = styles[index]?.textContent;
	if (content)
	{
		styleElement.textContent = content;
		doc.body.appendChild(styleElement);

		let css = styleElement.sheet.cssRules[0];
		if (css && css.selectorText[0] == '#' && css.style.display != 'none' && css.style.visibility != 'hidden')
		{
			visibleStyles.push(css.selectorText.substring(1));
		}
	}
	index++;
}
return visibleStyles;
");

		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.UpdateChat, "input", $@"
let talks = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > input'), maxTalks=talks.length;
let visible = {GetRegisteredJSFunctionName(99)}, nVisible = visible.length;
for (let index=0;index<maxTalks;index++) {{
	for (let index2=0;index2<nVisible;index2++) {{
		if (talks[index].id == visible[index2]) {{
			talks[index].value = input;
			break;
		}}
	}}
}}
");

		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.ClickSubmit, "", $@"
let buttons = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > button'), maxButtons=buttons.length;
let visible = {GetRegisteredJSFunctionName(99)}, nVisible = visible.length;
for (let index=0;index<maxButtons;index++) {{
	for (let index2=0;index2<nVisible;index2++) {{
		if (buttons[index].id == visible[index2]) {{
			buttons[index].click();
			break;
		}}
	}}
}}
");
		base.RegisterInGameFunctions(alreadyRegistered);
	}
}
