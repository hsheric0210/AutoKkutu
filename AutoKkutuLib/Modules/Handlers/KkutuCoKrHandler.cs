namespace AutoKkutuLib.Modules.Handlers;

internal class KkutuCoKrHandler : AbstractHandler
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] { new Uri("https://kkutu.co.kr/") };

	public override string HandlerName => "Kkutu.co.kr Handler";

	public override void UpdateChat(string input)
	{
		RegisterJSFunction(WriteInputFunc, "input", @"
var userMessages = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > input')
var maxIndex = userMessages.length, index = 0;
while (index < maxIndex) {{
    if (window.getComputedStyle(userMessages[index]).display != 'none') {{
		userMessages[index].value = input;
        break;
    }}
	index++;
}}
");

		EvaluateJS($"{GetRegisteredJSFunctionName(WriteInputFunc)}('{input}')");
	}

	public override void ClickSubmit()
	{
		RegisterJSFunction(ClickSubmitFunc, "", @"
var buttons = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > button')
var maxIndex = buttons.length, index = 0;
while (index < maxIndex) {{
    if (window.getComputedStyle(buttons[index]).display != 'none') {{
		buttons[index].click();
        break;
    }}
	index++;
}}
");

		EvaluateJS($"{GetRegisteredJSFunctionName(ClickSubmitFunc)}()");
	}
}
