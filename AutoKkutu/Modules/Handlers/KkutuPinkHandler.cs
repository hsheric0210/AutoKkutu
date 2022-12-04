using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.HandlerManager.Handler
{
	internal class KkutuPinkHandler : AbstractHandler
	{
		public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] { new Uri("https://kkutu.pink/") };

		public override string HandlerName => "Kkutu.pink Handler";
	}
}
