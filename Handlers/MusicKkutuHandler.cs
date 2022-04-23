using CefSharp.Wpf;

namespace AutoKkutu.Handlers
{
	internal partial class MusicKkutuHandler : CommonHandler
	{
		public MusicKkutuHandler(ChromiumWebBrowser browser) : base(browser)
		{
		}

		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?musickkutu\\.xyz.*$";

		public override string GetHandlerName() => "Musickkutu.xyz Handler";
	}
}
