namespace AutoKkutu.Handlers
{
	internal partial class KkutuCoKrHandler : CommonHandler
	{
		public override string GetSitePattern() => "(http:|https:)?(\\/\\/)?kkutu\\.co\\.kr\\/.*\\/game.*$";

		public override string GetHandlerName() => "Kkutu.co.kr Handler";

		public override void SendMessage(string input)
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

			EvaluateJS($"{GetRegisteredJSFunctionName(WriteInputFunc)}('{input}')");
			EvaluateJS($"{GetRegisteredJSFunctionName(ClickSubmitFunc)}()");
		}
	}
}
