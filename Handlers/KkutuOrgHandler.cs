namespace AutoKkutu.Handlers
{
	internal partial class KkutuOrgHandler : CommonHandler
	{
		protected override void UpdateChatInternal(string input) => EvaluateJS($"document.querySelectorAll('[id*=\"Talk\"]')[0].value='{input.Trim()}'");

		public override void PressSubmitButton() => EvaluateJS("document.getElementById('ChatBtn').click()");

		public override string GetSitePattern() => "(http:|https:)?(\\/\\/)?kkutu\\.org.*$";

		public override string GetHandlerName() => "Kkutu.org Handler";
	}
}
