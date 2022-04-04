using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	// TODO
	internal partial class KkutuOrgHandler : CommonHandler
	{
		public KkutuOrgHandler(ChromiumWebBrowser browser) : base(browser)
		{
		}

		public override string GetSiteURL() => "https://kkutu.org";
	}
}
