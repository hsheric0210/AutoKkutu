namespace AutoKkutuLib.Handlers;
public static class HandlerList
{
	private static readonly ISet<AbstractHandler> RegisteredHandlers = new HashSet<AbstractHandler>();

	public static void InitDefaultHandlers(JSEvaluator jsEvaluator)
	{
		RegisterHandler(new BFKkutuHandler(jsEvaluator));
		RegisterHandler(new KkutuCoKrHandler(jsEvaluator));
		RegisterHandler(new KkutuIoHandler(jsEvaluator));
		RegisterHandler(new KkutuOrgHandler(jsEvaluator));
		RegisterHandler(new KkutuPinkHandler(jsEvaluator));
		RegisterHandler(new MusicKkutuHandler(jsEvaluator));
	}

	public static void RegisterHandler(AbstractHandler handler) => RegisteredHandlers.Add(handler);

	public static AbstractHandler? GetByUri(Uri uri)
	{
		return (from handler in RegisteredHandlers
				where handler.UrlPattern.Any(baseUri => baseUri.IsBaseOf(uri))
				select handler).FirstOrDefault();
	}
}
