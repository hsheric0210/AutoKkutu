using AutoKkutuGui.Enterer;
using AutoKkutuGui.Plugin;
using AutoKkutuLib.Browser;

namespace ExamplePlugin;
internal class PluginMain : IPlugin
{
	public string PluginName => "ExamplePlugin";

	public IDomHandlerProvider? GetDomHandlerProvider(BrowserBase browser) => null;
	public IEntererProvider? GetEntererProvider() => null;
	public IWebSocketHandlerProvider? GetWebSocketHandlerProvider(BrowserBase browser) => null;
}
