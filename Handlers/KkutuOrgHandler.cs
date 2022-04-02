using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	internal partial class KkutuOrgHandler : CommonHandler
	{
		public KkutuOrgHandler(ChromiumWebBrowser browser) : base(browser)
		{
		}

		public override string GetSiteURL() => "https://kkutu.org";
	}
}
