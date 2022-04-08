using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	// TODO
	internal partial class KkutuPinkHandler : CommonHandler
	{
		public KkutuPinkHandler(ChromiumWebBrowser browser) : base(browser)
		{
		}

		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?kkutu\\.pink.*$";

		public override string GetHandlerName() => "Kkutu.pink Handler";
	}
}
