namespace AutoKkutu.Handler
{
	internal partial class MusicKkutuHandler : CommonHandler
	{
		public override string GetSitePattern() => "(http:|https:)?(\\/\\/)?musickkutu\\.xyz.*$";

		public override string GetHandlerName() => "Musickkutu.xyz Handler";
	}
}
