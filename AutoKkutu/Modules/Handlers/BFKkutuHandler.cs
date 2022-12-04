using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.Handlers;

internal class BFKkutuHandler : AbstractHandler
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] { new Uri("https://bfkkutu.kr/") };

	public override string HandlerName => "BFKkutu.kr Handler";

	public override void UpdateChat(string input)
	{
		RegisterJSFunction(WriteInputFunc, "input", @"
var chatFields = document.querySelectorAll('#Middle > div.ChatBox.Product > div.product-body > input')
var maxIndex = chatFields.length, index = 0;
while (index < maxIndex) {{
    if (window.getComputedStyle(chatFields[index]).display != 'none') {{
		chatFields[index].value = input;
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
