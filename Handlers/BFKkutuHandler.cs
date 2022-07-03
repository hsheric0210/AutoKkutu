namespace AutoKkutu.Handlers
{
	internal partial class BFKkutuHandler : CommonHandler
	{
		public override string GetSitePattern() => "(http:|https:)?(\\/\\/)?bfkkutu\\.kr\\/.*$";

		public override string GetHandlerName() => "BFKkutu.kr Handler";

		protected override void UpdateChatInternal(string input)
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
