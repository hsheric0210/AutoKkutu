namespace AutoKkutu.Handlers
{
	internal partial class MusicKkutuHandler : CommonHandler
	{
		public override string GetSiteURLPattern() => "(http:|https:)?(\\/\\/)?musickkutu\\.xyz.*$";

		public override string GetHandlerName() => "Musickkutu.xyz Handler";
	}
}
