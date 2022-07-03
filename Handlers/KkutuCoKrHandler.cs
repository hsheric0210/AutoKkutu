namespace AutoKkutu.Handlers
{
	internal partial class KkutuCoKrHandler : CommonHandler
	{
		public override string GetSitePattern() => "(http:|https:)?(\\/\\/)?kkutu\\.co\\.kr\\/.*\\/game.*$";

		public override string GetHandlerName() => "Kkutu.co.kr Handler";

		protected override void UpdateChatInternal(string input)
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

		public override void PressSubmitButton()
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
}
