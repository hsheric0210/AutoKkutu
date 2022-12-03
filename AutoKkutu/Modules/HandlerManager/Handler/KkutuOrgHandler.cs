namespace AutoKkutu.Modules.HandlerManager.Handler
{
	internal partial class KkutuOrgHandler : HandlerCore
	{
		protected override void UpdateChatInternal(string input) => EvaluateJS($"document.querySelectorAll('[id*=\"Talk\"]')[0].value='{input.Trim()}'");

		protected override void ClickSubmitButtonInternal() => EvaluateJS("document.getElementById('ChatBtn').click()");

		public override string GetSitePattern() => "(http:|https:)?(\\/\\/)?kkutu\\.org.*$";

		public override string GetHandlerName() => "Kkutu.org Handler";
	}
}
