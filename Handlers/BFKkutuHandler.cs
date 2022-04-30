namespace AutoKkutu.Handlers
{
	internal partial class BFKkutuHandler : CommonHandler
	{
		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?bfkkutu\\.kr\\/.*$";

		public override string GetHandlerName() => "BFKkutu.kr Handler";

		public override void SendMessage(string input)
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

			EvaluateJS($"{RegisteredJSFunctionName(WriteInputFunc)}('{input}')");
			EvaluateJS($"{RegisteredJSFunctionName(ClickSubmitFunc)}()");
		}
	}
}
