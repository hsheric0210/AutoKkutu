using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.HandlerManager.Handler
{
	internal partial class KkutuOrgHandler : AbstractHandler
	{
		public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] { new Uri("https://kkutu.org/") };

		public override string HandlerName => "Kkutu.org Handler";

		public override void UpdateChat(string input) => EvaluateJS($"document.querySelectorAll('[id*=\"Talk\"]')[0].value='{input.Trim()}'");

		public override void ClickSubmit() => EvaluateJS("document.getElementById('ChatBtn').click()");
	}
}
