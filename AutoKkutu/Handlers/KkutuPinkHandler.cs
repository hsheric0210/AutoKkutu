namespace AutoKkutu.Handlers
{
	internal partial class KkutuPinkHandler : CommonHandler
	{
		public override string GetSitePattern() => "(http:|https:)?(\\/\\/)?kkutu\\.pink.*$";

		public override string GetHandlerName() => "Kkutu.pink Handler";
	}
}
