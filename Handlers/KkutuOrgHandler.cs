namespace AutoKkutu.Handlers
{
	// TODO
	internal partial class KkutuOrgHandler : CommonHandler
	{
		public override void SendMessage(string input)
		{
			EvaluateJS($"document.querySelectorAll('[id*=\"Talk\"]')[0].value='{input.Trim()}'");
			EvaluateJS("document.getElementById('ChatBtn').click()");
		}

		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?kkutu\\.org.*$";

		public override string GetHandlerName() => "Kkutu.org Handler";
	}
}
